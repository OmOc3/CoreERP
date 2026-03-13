using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.MasterData;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken);
    Task<CategoryDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record CategoryDto(Guid Id, string Code, string Name, string? Description);

public sealed class SaveCategoryRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}

public sealed class CategoryService : ICategoryService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IValidator<SaveCategoryRequest> _validator;

    public CategoryService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IValidator<SaveCategoryRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.View);

        var query = _dbContext.ProductCategories
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
            _ => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
        };

        return await query
            .Select(x => new CategoryDto(x.Id, x.Code, x.Name, x.Description))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.View);

        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.View);

        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new CategoryDto(x.Id, x.Code, x.Name, x.Description))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
    }

    public async Task<Guid> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _dbContext.ProductCategories.AnyAsync(x => !x.IsDeleted && x.Code == code, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Category code '{code}' already exists.");
        }

        var entity = new ProductCategory(code, request.Name, request.Description);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);
        _dbContext.ProductCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ProductCategory), entity.Id.ToString(), "Create", null, entity, null, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _dbContext.ProductCategories.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        var before = new CategoryDto(entity.Id, entity.Code, entity.Name, entity.Description);
        var code = request.Code.Trim().ToUpperInvariant();

        var duplicate = await _dbContext.ProductCategories.AnyAsync(x => x.Id != id && !x.IsDeleted && x.Code == code, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"Category code '{code}' already exists.");
        }

        entity.Update(code, request.Name, request.Description);
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ProductCategory), entity.Id.ToString(), "Update", before, entity, null, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Categories.Manage);
        var entity = await _dbContext.ProductCategories.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        entity.SoftDelete(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ProductCategory), entity.Id.ToString(), "Delete", entity, null, null, cancellationToken);
    }
}
