using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.MasterData;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken);
    Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SaveProductRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveProductRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record ProductListItemDto(
    Guid Id,
    string Code,
    string Name,
    string SKU,
    string CategoryName,
    decimal SalePrice,
    decimal StandardCost,
    decimal ReorderLevel,
    bool IsActive);

public sealed record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string SKU,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? UnitOfMeasureId,
    string? UnitOfMeasureName,
    decimal ReorderLevel,
    decimal StandardCost,
    decimal SalePrice,
    bool IsStockTracked,
    bool IsActive);

public sealed class SaveProductRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid CategoryId { get; init; }
    public Guid? UnitOfMeasureId { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal StandardCost { get; init; }
    public decimal SalePrice { get; init; }
    public bool IsStockTracked { get; init; } = true;
    public bool IsActive { get; init; } = true;
}

public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StandardCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class ProductService : IProductService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IValidator<SaveProductRequest> _validator;

    public ProductService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IValidator<SaveProductRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.View);

        var query = _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                x.SKU.ToLower().Contains(search));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "sku" => request.SortDescending ? query.OrderByDescending(x => x.SKU) : query.OrderBy(x => x.SKU),
            _ => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
        };

        return await query
            .Select(x => new ProductListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.SKU,
                x.Category!.Name,
                x.SalePrice,
                x.StandardCost,
                x.ReorderLevel,
                x.IsActive))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.View);

        return await _dbContext.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.View);

        return await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.UnitOfMeasure)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ProductDto(
                x.Id,
                x.Code,
                x.Name,
                x.SKU,
                x.Description,
                x.CategoryId,
                x.Category!.Name,
                x.UnitOfMeasureId,
                x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : null,
                x.ReorderLevel,
                x.StandardCost,
                x.SalePrice,
                x.IsStockTracked,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
    }

    public async Task<Guid> CreateAsync(SaveProductRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureMasterReferencesAsync(request.CategoryId, request.UnitOfMeasureId, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        var sku = request.SKU.Trim().ToUpperInvariant();
        var duplicate = await _dbContext.Products.AnyAsync(
            x => !x.IsDeleted && (x.Code == code || x.SKU == sku),
            cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("A product with the same code or SKU already exists.");
        }

        var entity = new Product(
            code,
            request.Name,
            sku,
            request.CategoryId,
            request.UnitOfMeasureId,
            request.ReorderLevel,
            request.StandardCost,
            request.SalePrice,
            request.IsStockTracked,
            request.Description);
        entity.Update(
            code,
            request.Name,
            sku,
            request.CategoryId,
            request.UnitOfMeasureId,
            request.ReorderLevel,
            request.StandardCost,
            request.SalePrice,
            request.IsStockTracked,
            request.IsActive,
            request.Description);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.Products.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Product), entity.Id.ToString(), "Create", null, entity, null, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SaveProductRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureMasterReferencesAsync(request.CategoryId, request.UnitOfMeasureId, cancellationToken);

        var entity = await _dbContext.Products.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        var before = await GetAsync(id, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();
        var sku = request.SKU.Trim().ToUpperInvariant();

        var duplicate = await _dbContext.Products.AnyAsync(
            x => x.Id != id && !x.IsDeleted && (x.Code == code || x.SKU == sku),
            cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("A product with the same code or SKU already exists.");
        }

        entity.Update(
            code,
            request.Name,
            sku,
            request.CategoryId,
            request.UnitOfMeasureId,
            request.ReorderLevel,
            request.StandardCost,
            request.SalePrice,
            request.IsStockTracked,
            request.IsActive,
            request.Description);
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Product), entity.Id.ToString(), "Update", before, entity, null, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Products.Manage);
        var entity = await _dbContext.Products.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        entity.SoftDelete(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Product), entity.Id.ToString(), "Delete", entity, null, null, cancellationToken);
    }

    private async Task EnsureMasterReferencesAsync(Guid categoryId, Guid? unitOfMeasureId, CancellationToken cancellationToken)
    {
        var categoryExists = await _dbContext.ProductCategories.AnyAsync(x => x.Id == categoryId && !x.IsDeleted, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("Category was not found.");
        }

        if (!unitOfMeasureId.HasValue)
        {
            return;
        }

        var unitExists = await _dbContext.UnitsOfMeasure.AnyAsync(x => x.Id == unitOfMeasureId && !x.IsDeleted, cancellationToken);
        if (!unitExists)
        {
            throw new NotFoundException("Unit of measure was not found.");
        }
    }
}
