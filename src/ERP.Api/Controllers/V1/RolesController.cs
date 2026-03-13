using ERP.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IUserAdministrationService _service;

    public RolesController(IUserAdministrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetRoles(CancellationToken cancellationToken)
        => Ok(await _service.GetRolesAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetRole(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetRoleAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRole), new { version = "1.0", id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateRoleAsync(id, request, cancellationToken);
        return NoContent();
    }
}
