using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.MasterData;

public interface IBranchService
{
    Task<PagedResult<BranchDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken);
    Task<BranchDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SaveBranchRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveBranchRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record BranchDto(Guid Id, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive);

public sealed class SaveBranchRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class SaveBranchRequestValidator : AbstractValidator<SaveBranchRequest>
{
    public SaveBranchRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Address).MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Email).MaximumLength(128).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class BranchService : IBranchService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IValidator<SaveBranchRequest> _validator;

    public BranchService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IValidator<SaveBranchRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<BranchDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.View);

        var query = _dbContext.Branches
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        return await query
            .Select(x => new BranchDto(x.Id, x.Code, x.Name, x.Address, x.Phone, x.Email, x.IsActive))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.View);

        return await _dbContext.Branches
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.View);

        return await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new BranchDto(x.Id, x.Code, x.Name, x.Address, x.Phone, x.Email, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Branch was not found.");
    }

    public async Task<Guid> CreateAsync(SaveBranchRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _dbContext.Branches.AnyAsync(x => !x.IsDeleted && x.Code == code, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Branch code '{code}' already exists.");
        }

        var entity = new Branch(code, request.Name, request.Address, request.Phone, request.Email);
        entity.Update(code, request.Name, request.Address, request.Phone, request.Email, request.IsActive);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.Branches.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Branch), entity.Id.ToString(), "Create", null, entity, entity.Id, cancellationToken);

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SaveBranchRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _dbContext.Branches.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Branch was not found.");
        var before = new BranchDto(entity.Id, entity.Code, entity.Name, entity.Address, entity.Phone, entity.Email, entity.IsActive);
        var code = request.Code.Trim().ToUpperInvariant();

        var duplicate = await _dbContext.Branches.AnyAsync(x => x.Id != id && !x.IsDeleted && x.Code == code, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"Branch code '{code}' already exists.");
        }

        entity.Update(code, request.Name, request.Address, request.Phone, request.Email, request.IsActive);
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Branch), entity.Id.ToString(), "Update", before, entity, entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Branches.Manage);

        var entity = await _dbContext.Branches.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Branch was not found.");

        entity.SoftDelete(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Branch), entity.Id.ToString(), "Delete", entity, null, entity.Id, cancellationToken);
    }
}
