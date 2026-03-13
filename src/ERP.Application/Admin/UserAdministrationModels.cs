using ERP.Application.Common.Models;

namespace ERP.Application.Admin;

public interface IUserAdministrationService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(ListQuery request, CancellationToken cancellationToken);
    Task<UserDetailDto> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken);
    Task UpdateUserAsync(Guid id, SaveUserRequest request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<RoleDto> GetRoleAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateRoleAsync(SaveRoleRequest request, CancellationToken cancellationToken);
    Task UpdateRoleAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken);
}

public sealed record UserListItemDto(Guid Id, string UserName, string? Email, bool IsActive, IReadOnlyCollection<string> Roles, IReadOnlyCollection<Guid> BranchIds);
public sealed record UserDetailDto(Guid Id, string UserName, string? Email, bool IsActive, IReadOnlyCollection<string> Roles, IReadOnlyCollection<Guid> BranchIds, Guid? DefaultBranchId);
public sealed record RoleDto(Guid Id, string Name, string? Description, IReadOnlyCollection<string> PermissionCodes);
public sealed record PermissionDto(Guid Id, string Module, string Code, string Name, string? Description);

public sealed class SaveUserRequest
{
    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Password { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<Guid> BranchIds { get; init; } = Array.Empty<Guid>();
    public Guid? DefaultBranchId { get; init; }
}

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class SaveRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyCollection<string> PermissionCodes { get; init; } = Array.Empty<string>();
}
