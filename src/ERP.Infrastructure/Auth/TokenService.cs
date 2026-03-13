using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERP.Application.Auth;
using ERP.Application.Common.Contracts;
using ERP.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly IOptions<ErpOptions> _options;
    private readonly IClock _clock;

    public TokenService(IOptions<ErpOptions> options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public Task<TokenEnvelope> CreateTokenAsync(
        Guid userId,
        string userName,
        string? email,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        IReadOnlyCollection<Guid> branchIds,
        Guid? defaultBranchId,
        CancellationToken cancellationToken)
    {
        var expiresAt = _clock.UtcNow.AddMinutes(_options.Value.Jwt.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.Jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        claims.AddRange(branchIds.Select(branchId => new Claim("branch", branchId.ToString())));

        if (defaultBranchId.HasValue)
        {
            claims.Add(new Claim("default_branch", defaultBranchId.Value.ToString()));
        }

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Value.Jwt.Issuer,
            audience: _options.Value.Jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();

        var user = new AuthenticatedUserDto(userId, userName, email, roles, permissions, branchIds, defaultBranchId);
        return Task.FromResult(new TokenEnvelope(accessToken, refreshToken, expiresAt, user));
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
