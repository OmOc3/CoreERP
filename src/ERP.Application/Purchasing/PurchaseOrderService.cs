using ERP.Application.Approvals;
using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Purchasing;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListItemDto>> GetPagedAsync(PurchaseOrderQuery request, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SavePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken);
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class PurchaseOrderQuery : BranchScopedQuery
{
    public Guid? SupplierId { get; init; }
    public PurchaseOrderStatus? Status { get; init; }
}

public sealed record PurchaseOrderListItemDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDateUtc,
    PurchaseOrderStatus Status,
    decimal TotalAmount);

public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    string? Description);

public sealed record PurchaseOrderDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDateUtc,
    DateTime? ExpectedDateUtc,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyCollection<PurchaseOrderLineDto> Lines);

public sealed class SavePurchaseOrderRequest
{
    public Guid BranchId { get; init; }
    public Guid SupplierId { get; init; }
    public DateTime OrderDateUtc { get; init; }
    public DateTime? ExpectedDateUtc { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<SavePurchaseOrderLineRequest> Lines { get; init; } = Array.Empty<SavePurchaseOrderLineRequest>();
}

public sealed class SavePurchaseOrderLineRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal TaxPercent { get; init; }
    public string? Description { get; init; }
}

public sealed class SavePurchaseOrderRequestValidator : AbstractValidator<SavePurchaseOrderRequest>
{
    public SavePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new SavePurchaseOrderLineRequestValidator());
    }
}

public sealed class SavePurchaseOrderLineRequestValidator : AbstractValidator<SavePurchaseOrderLineRequest>
{
    public SavePurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IApprovalService _approvalService;
    private readonly IValidator<SavePurchaseOrderRequest> _validator;

    public PurchaseOrderService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IApprovalService approvalService,
        IValidator<SavePurchaseOrderRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _approvalService = approvalService;
        _validator = validator;
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>> GetPagedAsync(PurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.View);

        var query = _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Where(x => !x.IsDeleted);

        if (request.BranchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(request.BranchId.Value);
            query = query.Where(x => x.BranchId == request.BranchId.Value);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            query = query.Where(x => branchIds.Contains(x.BranchId));
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.OrderDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.OrderDateUtc <= request.DateToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Number.ToLower().Contains(search) ||
                x.Supplier!.Name.ToLower().Contains(search));
        }

        query = query.OrderByDescending(x => x.OrderDateUtc);

        return await query
            .Select(x => new PurchaseOrderListItemDto(
                x.Id,
                x.Number,
                x.BranchId,
                x.Branch!.Name,
                x.SupplierId,
                x.Supplier!.Name,
                x.OrderDateUtc,
                x.Status,
                x.TotalAmount))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.View);

        var entity = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase order was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);

        return new PurchaseOrderDto(
            entity.Id,
            entity.Number,
            entity.BranchId,
            entity.Branch!.Name,
            entity.SupplierId,
            entity.Supplier!.Name,
            entity.OrderDateUtc,
            entity.ExpectedDateUtc,
            entity.Status,
            entity.TotalAmount,
            entity.Notes,
            entity.Lines.Select(x => new PurchaseOrderLineDto(
                x.Id,
                x.ProductId,
                x.Product!.Code,
                x.Product.Name,
                x.OrderedQuantity,
                x.ReceivedQuantity,
                x.UnitPrice,
                x.DiscountPercent,
                x.TaxPercent,
                x.LineTotal,
                x.Description)).ToList());
    }

    public async Task<Guid> CreateAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureReferencesAsync(request.BranchId, request.SupplierId, request.Lines.Select(x => x.ProductId).ToList(), cancellationToken);

        var number = await _numberSequenceService.NextAsync("PO", cancellationToken);
        var entity = new PurchaseOrder(number, request.SupplierId, request.BranchId, request.OrderDateUtc, request.ExpectedDateUtc, request.Notes);
        entity.ReplaceLines(request.Lines.Select(x =>
            new PurchaseOrderLine(x.ProductId, x.Quantity, x.UnitPrice, x.DiscountPercent, x.TaxPercent, x.Description)));
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.PurchaseOrders.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(PurchaseOrder), entity.Id.ToString(), "Create", null, entity, entity.BranchId, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureReferencesAsync(request.BranchId, request.SupplierId, request.Lines.Select(x => x.ProductId).ToList(), cancellationToken);

        var entity = await _dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase order was not found.");
        var before = await GetAsync(id, cancellationToken);

        entity.ReplaceLines(request.Lines.Select(x =>
            new PurchaseOrderLine(x.ProductId, x.Quantity, x.UnitPrice, x.DiscountPercent, x.TaxPercent, x.Description)));
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(PurchaseOrder), entity.Id.ToString(), "Update", before, entity, entity.BranchId, cancellationToken);
    }

    public async Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.Manage);

        var entity = await _dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase order was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);

        entity.SubmitForApproval();
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        var result = await _approvalService.CreateRequestsAsync(
            ApprovalDocumentType.PurchaseOrder,
            entity.Id,
            entity.BranchId,
            entity.TotalAmount,
            cancellationToken);

        if (!result.HasApprovalRequests)
        {
            entity.Approve();
            await _auditService.LogAsync(nameof(PurchaseOrder), entity.Id.ToString(), "AutoApprove", null, entity, entity.BranchId, cancellationToken);
        }
        else
        {
            await _auditService.LogAsync(nameof(PurchaseOrder), entity.Id.ToString(), "SubmitForApproval", null, entity, entity.BranchId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.Manage);

        var entity = await _dbContext.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase order was not found.");
        _currentUserService.EnsureBranchAccess(entity.BranchId);

        entity.Cancel();
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(PurchaseOrder), entity.Id.ToString(), "Cancel", null, entity, entity.BranchId, cancellationToken);
    }

    private async Task EnsureReferencesAsync(Guid branchId, Guid supplierId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == branchId && !x.IsDeleted, cancellationToken);
        if (!branchExists)
        {
            throw new NotFoundException("Branch was not found.");
        }

        var supplierExists = await _dbContext.Suppliers.AnyAsync(x => x.Id == supplierId && !x.IsDeleted, cancellationToken);
        if (!supplierExists)
        {
            throw new NotFoundException("Supplier was not found.");
        }

        var availableProducts = await _dbContext.Products.CountAsync(x => productIds.Contains(x.Id) && !x.IsDeleted, cancellationToken);
        if (availableProducts != productIds.Distinct().Count())
        {
            throw new NotFoundException("One or more products were not found.");
        }
    }
}
