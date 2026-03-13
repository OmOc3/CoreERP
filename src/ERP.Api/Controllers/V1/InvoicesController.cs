using ERP.Application.Common.Models;
using ERP.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpGet("purchase")]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> GetPurchase([FromQuery] InvoiceQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPurchaseInvoicesAsync(request, cancellationToken));

    [HttpGet("purchase/{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetPurchase(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetPurchaseInvoiceAsync(id, cancellationToken));

    [HttpPost("purchase")]
    public async Task<ActionResult<Guid>> CreatePurchase([FromBody] SavePurchaseInvoiceRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreatePurchaseInvoiceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPurchase), new { version = "1.0", id }, id);
    }

    [HttpGet("sales")]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> GetSales([FromQuery] InvoiceQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetSalesInvoicesAsync(request, cancellationToken));

    [HttpGet("sales/{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetSales(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetSalesInvoiceAsync(id, cancellationToken));

    [HttpPost("sales")]
    public async Task<ActionResult<Guid>> CreateSales([FromBody] SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateSalesInvoiceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSales), new { version = "1.0", id }, id);
    }
}
