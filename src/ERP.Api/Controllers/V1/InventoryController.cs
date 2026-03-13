using ERP.Application.Common.Models;
using ERP.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service)
    {
        _service = service;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<PagedResult<StockBalanceDto>>> Balances([FromQuery] StockBalanceQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetStockBalancesAsync(request, cancellationToken));

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<InventoryMovementDto>>> Movements([FromQuery] InventoryMovementQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetMovementsAsync(request, cancellationToken));

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyCollection<LowStockItemDto>>> LowStock([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => Ok(await _service.GetLowStockAsync(branchId, cancellationToken));

    [HttpPost("adjustments")]
    public async Task<IActionResult> Adjust([FromBody] StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        await _service.AdjustStockAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> Transfer([FromBody] StockTransferRequest request, CancellationToken cancellationToken)
    {
        await _service.TransferStockAsync(request, cancellationToken);
        return NoContent();
    }
}
