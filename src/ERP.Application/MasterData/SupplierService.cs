using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.MasterData;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken);
    Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record SupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    int PaymentTermsDays,
    bool IsActive);

public sealed class SaveSupplierRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? TaxNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public int PaymentTermsDays { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class SaveSupplierRequestValidator : AbstractValidator<SaveSupplierRequest>
{
    public SaveSupplierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).MaximumLength(128).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Address).MaximumLength(256);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public sealed class SupplierService : ISupplierService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IValidator<SaveSupplierRequest> _validator;

    public SupplierService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IValidator<SaveSupplierRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.View);

        var query = _dbContext.Suppliers.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            _ => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
        };

        return await query
            .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.TaxNumber, x.Email, x.Phone, x.Address, x.PaymentTermsDays, x.IsActive))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LookupDto>> GetLookupAsync(CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.View);

        return await _dbContext.Suppliers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.View);

        return await _dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.TaxNumber, x.Email, x.Phone, x.Address, x.PaymentTermsDays, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Supplier was not found.");
    }

    public async Task<Guid> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _dbContext.Suppliers.AnyAsync(x => !x.IsDeleted && x.Code == code, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Supplier code '{code}' already exists.");
        }

        var entity = new Supplier(code, request.Name, request.TaxNumber, request.Email, request.Phone, request.Address, request.PaymentTermsDays);
        entity.Update(code, request.Name, request.TaxNumber, request.Email, request.Phone, request.Address, request.PaymentTermsDays, request.IsActive);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.Suppliers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Supplier), entity.Id.ToString(), "Create", null, entity, null, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.Manage);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _dbContext.Suppliers.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Supplier was not found.");
        var before = await GetAsync(id, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();

        var duplicate = await _dbContext.Suppliers.AnyAsync(x => x.Id != id && !x.IsDeleted && x.Code == code, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"Supplier code '{code}' already exists.");
        }

        entity.Update(code, request.Name, request.TaxNumber, request.Email, request.Phone, request.Address, request.PaymentTermsDays, request.IsActive);
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Supplier), entity.Id.ToString(), "Update", before, entity, null, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Suppliers.Manage);
        var entity = await _dbContext.Suppliers.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Supplier was not found.");
        entity.SoftDelete(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Supplier), entity.Id.ToString(), "Delete", entity, null, null, cancellationToken);
    }
}
