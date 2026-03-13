using ERP.Application.Admin;
using ERP.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get([FromQuery] AuditLogQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));
}
