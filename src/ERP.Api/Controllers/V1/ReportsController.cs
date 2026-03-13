using ERP.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet("sales-summary")]
    public async Task<ActionResult<IReadOnlyCollection<SalesSummaryRowDto>>> SalesSummary([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetSalesSummaryAsync(request, cancellationToken));

    [HttpGet("sales-summary/excel")]
    public async Task<IActionResult> SalesSummaryExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportSalesSummaryExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sales-summary.xlsx");

    [HttpGet("sales-summary/pdf")]
    public async Task<IActionResult> SalesSummaryPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportSalesSummaryPdfAsync(request, cancellationToken)).Content, "application/pdf", "sales-summary.pdf");

    [HttpGet("purchase-summary")]
    public async Task<ActionResult<IReadOnlyCollection<PurchaseSummaryRowDto>>> PurchaseSummary([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetPurchaseSummaryAsync(request, cancellationToken));

    [HttpGet("purchase-summary/excel")]
    public async Task<IActionResult> PurchaseSummaryExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportPurchaseSummaryExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "purchase-summary.xlsx");

    [HttpGet("purchase-summary/pdf")]
    public async Task<IActionResult> PurchaseSummaryPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportPurchaseSummaryPdfAsync(request, cancellationToken)).Content, "application/pdf", "purchase-summary.pdf");

    [HttpGet("inventory-valuation")]
    public async Task<ActionResult<IReadOnlyCollection<InventoryValuationRowDto>>> InventoryValuation([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetInventoryValuationAsync(request, cancellationToken));

    [HttpGet("inventory-valuation/excel")]
    public async Task<IActionResult> InventoryValuationExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportInventoryValuationExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory-valuation.xlsx");

    [HttpGet("inventory-valuation/pdf")]
    public async Task<IActionResult> InventoryValuationPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportInventoryValuationPdfAsync(request, cancellationToken)).Content, "application/pdf", "inventory-valuation.pdf");

    [HttpGet("stock-movement")]
    public async Task<ActionResult<IReadOnlyCollection<StockMovementRowDto>>> StockMovement([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetStockMovementAsync(request, cancellationToken));

    [HttpGet("stock-movement/excel")]
    public async Task<IActionResult> StockMovementExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportStockMovementExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "stock-movement.xlsx");

    [HttpGet("stock-movement/pdf")]
    public async Task<IActionResult> StockMovementPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportStockMovementPdfAsync(request, cancellationToken)).Content, "application/pdf", "stock-movement.pdf");

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyCollection<LowStockReportRowDto>>> LowStock([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetLowStockAsync(request, cancellationToken));

    [HttpGet("low-stock/excel")]
    public async Task<IActionResult> LowStockExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportLowStockExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "low-stock.xlsx");

    [HttpGet("low-stock/pdf")]
    public async Task<IActionResult> LowStockPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportLowStockPdfAsync(request, cancellationToken)).Content, "application/pdf", "low-stock.pdf");

    [HttpGet("receivables")]
    public async Task<ActionResult<IReadOnlyCollection<ReceivableRowDto>>> Receivables([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetReceivablesAsync(request, cancellationToken));

    [HttpGet("receivables/excel")]
    public async Task<IActionResult> ReceivablesExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportReceivablesExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "receivables.xlsx");

    [HttpGet("receivables/pdf")]
    public async Task<IActionResult> ReceivablesPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportReceivablesPdfAsync(request, cancellationToken)).Content, "application/pdf", "receivables.pdf");

    [HttpGet("payables")]
    public async Task<ActionResult<IReadOnlyCollection<PayableRowDto>>> Payables([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.GetPayablesAsync(request, cancellationToken));

    [HttpGet("payables/excel")]
    public async Task<IActionResult> PayablesExcel([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportPayablesExcelAsync(request, cancellationToken)).Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "payables.xlsx");

    [HttpGet("payables/pdf")]
    public async Task<IActionResult> PayablesPdf([FromQuery] ReportFilterRequest request, CancellationToken cancellationToken)
        => File((await _service.ExportPayablesPdfAsync(request, cancellationToken)).Content, "application/pdf", "payables.pdf");
}
