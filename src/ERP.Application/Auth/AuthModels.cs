namespace ERP.Application.Auth;

public interface IAuthService
{
    Task<TokenEnvelope> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<TokenEnvelope> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken);
}

public sealed class LoginRequest
{
    public string UserNameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record AuthenticatedUserDto(
    Guid Id,
    string UserName,
    string? Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<Guid> BranchIds,
    Guid? DefaultBranchId);

public sealed record TokenEnvelope(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUserDto User);
