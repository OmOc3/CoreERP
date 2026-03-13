using ERP.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] Guid? branchId, [FromQuery] DateTime? dateFromUtc, [FromQuery] DateTime? dateToUtc, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(branchId, dateFromUtc, dateToUtc, cancellationToken));
}
