using System.Data;
using ClosedXML.Excel;
using ERP.Application.Common.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Infrastructure.Exports;

public sealed class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel<T>(string worksheetName, IReadOnlyCollection<T> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName);

        var properties = typeof(T).GetProperties();
        for (var columnIndex = 0; columnIndex < properties.Length; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = properties[columnIndex].Name;
            worksheet.Cell(1, columnIndex + 1).Style.Font.Bold = true;
            worksheet.Cell(1, columnIndex + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < properties.Length; columnIndex++)
            {
                worksheet.Cell(rowIndex, columnIndex + 1).Value = properties[columnIndex].GetValue(row)?.ToString();
            }

            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(string title, IEnumerable<KeyValuePair<string, string>> filters, IEnumerable<string> columns, IEnumerable<IReadOnlyCollection<string>> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(column =>
                    {
                        column.Item().Text(title).FontSize(18).SemiBold();
                        foreach (var filter in filters)
                        {
                            column.Item().Text($"{filter.Key}: {filter.Value}");
                        }
                    });

                    page.Content().Table(table =>
                    {
                        var columnList = columns.ToList();
                        table.ColumnsDefinition(definition =>
                        {
                            foreach (var _ in columnList)
                            {
                                definition.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var column in columnList)
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(column).SemiBold();
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
                            }
                        }
                    });
                });
            })
            .GeneratePdf();
    }
}
