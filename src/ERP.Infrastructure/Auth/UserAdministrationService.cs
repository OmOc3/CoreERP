using ERP.Application.Admin;
using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Auth;

public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IValidator<SaveUserRequest> _userValidator;
    private readonly IValidator<ResetPasswordRequest> _passwordValidator;
    private readonly IValidator<SaveRoleRequest> _roleValidator;

    public UserAdministrationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IValidator<SaveUserRequest> userValidator,
        IValidator<ResetPasswordRequest> passwordValidator,
        IValidator<SaveRoleRequest> roleValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _userValidator = userValidator;
        _passwordValidator = passwordValidator;
        _roleValidator = roleValidator;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Users.View);

        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.UserName!.ToLower().Contains(search) || (x.Email != null && x.Email.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(x => x.UserName)
            .Skip((request.NormalizedPageNumber - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var branches = await _dbContext.UserBranchAccesses
                .Where(x => x.UserId == user.Id && !x.IsDeleted)
                .Select(x => x.BranchId)
                .ToListAsync(cancellationToken);
            items.Add(new UserListItemDto(user.Id, user.UserName ?? string.Empty, user.Email, user.IsActive, roles.ToArray(), branches));
        }

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            PageNumber = request.NormalizedPageNumber,
            PageSize = request.NormalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserDetailDto> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Users.View);

        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        var roles = await _userManager.GetRolesAsync(user);
        var branches = await _dbContext.UserBranchAccesses
            .Where(x => x.UserId == id && !x.IsDeleted)
            .Select(x => x.BranchId)
            .ToListAsync(cancellationToken);

        return new UserDetailDto(user.Id, user.UserName ?? string.Empty, user.Email, user.IsActive, roles.ToArray(), branches, user.DefaultBranchId);
    }

    public async Task<Guid> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Users.Manage);
        await _userValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ConflictException("Password is required when creating a user.");
        }

        await EnsureRolesAndBranchesAsync(request.Roles, request.BranchIds, cancellationToken);

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            IsActive = request.IsActive,
            EmailConfirmed = true,
            DefaultBranchId = request.DefaultBranchId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRolesAsync(user, request.Roles);
        await ReplaceBranchAccessAsync(user.Id, request.BranchIds, request.DefaultBranchId, cancellationToken);
        await _auditService.LogAsync(nameof(ApplicationUser), user.Id.ToString(), "Create", null, new { user.UserName, user.Email, request.Roles, request.BranchIds }, request.DefaultBranchId, cancellationToken);
        return user.Id;
    }

    public async Task UpdateUserAsync(Guid id, SaveUserRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Users.Manage);
        await _userValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureRolesAndBranchesAsync(request.Roles, request.BranchIds, cancellationToken);

        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        var before = await GetUserAsync(id, cancellationToken);

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.DefaultBranchId = request.DefaultBranchId;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ConflictException(string.Join("; ", updateResult.Errors.Select(x => x.Description)));
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = existingRoles.Except(request.Roles).ToList();
        var rolesToAdd = request.Roles.Except(existingRoles).ToList();
        if (rolesToRemove.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        if (rolesToAdd.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd);
        }

        await ReplaceBranchAccessAsync(user.Id, request.BranchIds, request.DefaultBranchId, cancellationToken);
        await _auditService.LogAsync(nameof(ApplicationUser), user.Id.ToString(), "Update", before, new { user.UserName, user.Email, request.Roles, request.BranchIds }, request.DefaultBranchId, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Users.Manage);
        await _passwordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _auditService.LogAsync(nameof(ApplicationUser), user.Id.ToString(), "ResetPassword", null, new { user.UserName }, user.DefaultBranchId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Roles.View);

        var roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var roleIds = roles.Select(x => x.Id).ToList();
        var rolePermissions = await _dbContext.RolePermissions
            .Include(x => x.Permission)
            .Where(x => roleIds.Contains(x.RoleId))
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleDto(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                rolePermissions.Where(x => x.RoleId == role.Id).Select(x => x.Permission!.Code).OrderBy(x => x).ToList()))
            .ToList();
    }

    public async Task<RoleDto> GetRoleAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Roles.View);
        var role = await _roleManager.Roles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");

        var permissions = await _dbContext.RolePermissions
            .Include(x => x.Permission)
            .Where(x => x.RoleId == id)
            .Select(x => x.Permission!.Code)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new RoleDto(role.Id, role.Name ?? string.Empty, role.Description, permissions);
    }

    public async Task<Guid> CreateRoleAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Roles.Manage);
        await _roleValidator.ValidateAndThrowAsync(request, cancellationToken);

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await ReplaceRolePermissionsAsync(role.Id, request.PermissionCodes, cancellationToken);
        await _auditService.LogAsync(nameof(ApplicationRole), role.Id.ToString(), "Create", null, request, null, cancellationToken);
        return role.Id;
    }

    public async Task UpdateRoleAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Roles.Manage);
        await _roleValidator.ValidateAndThrowAsync(request, cancellationToken);

        var role = await _roleManager.Roles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");
        var before = await GetRoleAsync(id, cancellationToken);

        role.Name = request.Name;
        role.Description = request.Description;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await ReplaceRolePermissionsAsync(role.Id, request.PermissionCodes, cancellationToken);
        await _auditService.LogAsync(nameof(ApplicationRole), role.Id.ToString(), "Update", before, request, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Roles.View);

        return await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Code)
            .Select(x => new PermissionDto(x.Id, x.Module, x.Code, x.Name, x.Description))
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureRolesAndBranchesAsync(IReadOnlyCollection<string> roles, IReadOnlyCollection<Guid> branchIds, CancellationToken cancellationToken)
    {
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                throw new NotFoundException($"Role '{role}' was not found.");
            }
        }

        if (branchIds.Count == 0)
        {
            return;
        }

        var branchesFound = await _dbContext.Branches.CountAsync(x => branchIds.Contains(x.Id) && !x.IsDeleted, cancellationToken);
        if (branchesFound != branchIds.Distinct().Count())
        {
            throw new NotFoundException("One or more branches were not found.");
        }
    }

    private async Task ReplaceBranchAccessAsync(Guid userId, IReadOnlyCollection<Guid> branchIds, Guid? defaultBranchId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserBranchAccesses.Where(x => x.UserId == userId && !x.IsDeleted).ToListAsync(cancellationToken);
        foreach (var item in existing)
        {
            _dbContext.UserBranchAccesses.Remove(item);
        }

        foreach (var branchId in branchIds.Distinct())
        {
            var access = new UserBranchAccess
            {
                UserId = userId,
                BranchId = branchId,
                IsDefault = defaultBranchId == branchId
            };
            access.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);
            _dbContext.UserBranchAccesses.Add(access);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .Where(x => permissionCodes.Contains(x.Code) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (permissions.Count != permissionCodes.Distinct().Count())
        {
            throw new NotFoundException("One or more permissions were not found.");
        }

        var existing = await _dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        _dbContext.RolePermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            _dbContext.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
