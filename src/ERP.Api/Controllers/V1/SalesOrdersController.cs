using ERP.Application.Common.Models;
using ERP.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/sales-orders")]
public sealed class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _service;

    public SalesOrdersController(ISalesOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesOrderListItemDto>>> Get([FromQuery] SalesOrderQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SaveSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1.0", id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveSalesOrderRequest request, CancellationToken cancellationToken)
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
