namespace ERP.Application.Common.Contracts;

public interface IReportExportService
{
    byte[] ExportToExcel<T>(string worksheetName, IReadOnlyCollection<T> rows);
    byte[] ExportToPdf(string title, IEnumerable<KeyValuePair<string, string>> filters, IEnumerable<string> columns, IEnumerable<IReadOnlyCollection<string>> rows);
}
