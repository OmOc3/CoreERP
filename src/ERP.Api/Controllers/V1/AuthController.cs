using ERP.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenEnvelope>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenEnvelope>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.RefreshAsync(request, cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(CancellationToken cancellationToken)
        => Ok(await _authService.GetCurrentUserAsync(cancellationToken));
}
