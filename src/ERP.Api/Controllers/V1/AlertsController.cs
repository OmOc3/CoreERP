using ERP.Application.Common.Models;
using ERP.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _service;

    public AlertsController(IAlertService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AlertDto>>> Get([FromQuery] AlertQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _service.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }
}
