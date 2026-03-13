using System.Security.Claims;
using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CurrentUserContext? _cachedUser;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserContext User => _cachedUser ??= BuildUser();

    public Guid GetRequiredUserId()
    {
        EnsureAuthenticated();
        return User.UserId!.Value;
    }

    public void EnsureAuthenticated()
    {
        if (!User.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required.");
        }
    }

    public void EnsurePermission(string permission)
    {
        EnsureAuthenticated();
        if (User.IsAdministrator)
        {
            return;
        }

        if (!User.Permissions.Contains(permission))
        {
            throw new ForbiddenException($"Permission '{permission}' is required.");
        }
    }

    public void EnsureBranchAccess(Guid branchId)
    {
        EnsureAuthenticated();
        if (User.IsAdministrator)
        {
            return;
        }

        if (!User.BranchIds.Contains(branchId))
        {
            throw new ForbiddenException("You do not have access to the selected branch.");
        }
    }

    private CurrentUserContext BuildUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var principal = httpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUserContext();
        }

        var roles = principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).Distinct().ToList();
        var permissions = principal.Claims.Where(x => x.Type == "permission").Select(x => x.Value).Distinct().ToList();
        var branchIds = principal.Claims
            .Where(x => x.Type == "branch")
            .Select(x => Guid.TryParse(x.Value, out var branchId) ? branchId : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        Guid? defaultBranchId = null;
        var defaultBranchClaim = principal.FindFirst("default_branch")?.Value;
        if (Guid.TryParse(defaultBranchClaim, out var parsedBranchId))
        {
            defaultBranchId = parsedBranchId;
        }

        return new CurrentUserContext
        {
            UserId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            UserName = principal.FindFirstValue(ClaimTypes.Name),
            Email = principal.FindFirstValue(ClaimTypes.Email),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            Roles = roles,
            Permissions = permissions,
            BranchIds = branchIds,
            DefaultBranchId = defaultBranchId,
            IsAdministrator = roles.Contains(SystemRoles.Admin)
        };
    }
}
