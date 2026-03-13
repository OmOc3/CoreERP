using ERP.Application.Admin;
using ERP.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdministrationService _service;

    public UsersController(IUserAdministrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers([FromQuery] ListQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetUsersAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetUser(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetUserAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { version = "1.0", id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateUserAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _service.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
