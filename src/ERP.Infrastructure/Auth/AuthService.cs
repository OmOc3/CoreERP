using ERP.Application.Auth;
using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ErpDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<ErpOptions> _options;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ErpDbContext dbContext,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IClock clock,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ErpOptions> options,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _clock = clock;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    public async Task<TokenEnvelope> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalized = request.UserNameOrEmail.Trim();
        var user = await _userManager.Users.SingleOrDefaultAsync(
            x => x.UserName == normalized || x.Email == normalized,
            cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new ForbiddenException("Invalid username or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new ForbiddenException("Invalid username or password.");
        }

        return await CreateAndPersistTokensAsync(user, null, cancellationToken);
    }

    public async Task<TokenEnvelope> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existingToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
            x => x.Token == request.RefreshToken && !x.IsDeleted,
            cancellationToken)
            ?? throw new ForbiddenException("Refresh token is invalid.");

        if (!existingToken.IsActive)
        {
            throw new ForbiddenException("Refresh token has expired or was revoked.");
        }

        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == existingToken.UserId, cancellationToken)
            ?? throw new ForbiddenException("User account was not found.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        var tokenEnvelope = await CreateAndPersistTokensAsync(user, existingToken, cancellationToken);
        return tokenEnvelope;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var existingToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
            x => x.Token == refreshToken && !x.IsDeleted,
            cancellationToken);

        if (existingToken == null)
        {
            return;
        }

        existingToken.RevokedAtUtc = _clock.UtcNow;
        existingToken.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName ?? "system");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsureAuthenticated();
        var user = _currentUserService.User;
        return Task.FromResult(new AuthenticatedUserDto(
            user.UserId!.Value,
            user.UserName ?? string.Empty,
            user.Email,
            user.Roles,
            user.Permissions,
            user.BranchIds,
            user.DefaultBranchId));
    }

    private async Task<TokenEnvelope> CreateAndPersistTokensAsync(ApplicationUser user, RefreshToken? existingToken, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _dbContext.RolePermissions
            .Where(x => _dbContext.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == x.RoleId))
            .Select(x => x.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
        var branchAccess = await _dbContext.UserBranchAccesses
            .Where(x => x.UserId == user.Id && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ToListAsync(cancellationToken);

        var envelope = await _tokenService.CreateTokenAsync(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email,
            roles,
            permissions,
            branchAccess.Select(x => x.BranchId).ToList(),
            user.DefaultBranchId ?? branchAccess.FirstOrDefault(x => x.IsDefault)?.BranchId,
            cancellationToken);

        if (existingToken != null)
        {
            existingToken.RevokedAtUtc = _clock.UtcNow;
            existingToken.ReplacedByToken = envelope.RefreshToken;
            existingToken.SetUpdateAudit(_clock.UtcNow, user.UserName);
        }

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = envelope.RefreshToken,
            ExpiresAtUtc = _clock.UtcNow.AddDays(_options.Value.Jwt.RefreshTokenDays),
            CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
        };
        refreshToken.SetCreationAudit(_clock.UtcNow, user.UserName);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return envelope;
    }
}
