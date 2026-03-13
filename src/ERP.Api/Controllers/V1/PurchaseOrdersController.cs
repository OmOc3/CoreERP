using ERP.Application.Common.Models;
using ERP.Application.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/purchase-orders")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderListItemDto>>> Get([FromQuery] PurchaseOrderQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1.0", id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _service.SubmitForApprovalAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
