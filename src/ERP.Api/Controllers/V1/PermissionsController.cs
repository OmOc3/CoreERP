using ERP.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IUserAdministrationService _service;

    public PermissionsController(IUserAdministrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
        => Ok(await _service.GetPermissionsAsync(cancellationToken));
}
