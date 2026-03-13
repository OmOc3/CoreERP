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

namespace ERP.Application.Sales;

public interface ISalesOrderService
{
    Task<PagedResult<SalesOrderListItemDto>> GetPagedAsync(SalesOrderQuery request, CancellationToken cancellationToken);
    Task<SalesOrderDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(SaveSalesOrderRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveSalesOrderRequest request, CancellationToken cancellationToken);
    Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken);
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class SalesOrderQuery : BranchScopedQuery
{
    public Guid? CustomerId { get; init; }
    public SalesOrderStatus? Status { get; init; }
}

public sealed record SalesOrderListItemDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDateUtc,
    SalesOrderStatus Status,
    decimal TotalAmount);

public sealed record SalesOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal OrderedQuantity,
    decimal DeliveredQuantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    string? Description);

public sealed record SalesOrderDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDateUtc,
    DateTime? DueDateUtc,
    SalesOrderStatus Status,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyCollection<SalesOrderLineDto> Lines);

public sealed class SaveSalesOrderRequest
{
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime OrderDateUtc { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<SaveSalesOrderLineRequest> Lines { get; init; } = Array.Empty<SaveSalesOrderLineRequest>();
}

public sealed class SaveSalesOrderLineRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal TaxPercent { get; init; }
    public string? Description { get; init; }
}

public sealed class SaveSalesOrderRequestValidator : AbstractValidator<SaveSalesOrderRequest>
{
    public SaveSalesOrderRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new SaveSalesOrderLineRequestValidator());
    }
}

public sealed class SaveSalesOrderLineRequestValidator : AbstractValidator<SaveSalesOrderLineRequest>
{
    public SaveSalesOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}

public sealed class SalesOrderService : ISalesOrderService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IApprovalService _approvalService;
    private readonly IValidator<SaveSalesOrderRequest> _validator;

    public SalesOrderService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IApprovalService approvalService,
        IValidator<SaveSalesOrderRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _approvalService = approvalService;
        _validator = validator;
    }

    public async Task<PagedResult<SalesOrderListItemDto>> GetPagedAsync(SalesOrderQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.View);

        var query = _dbContext.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
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

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
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
                x.Customer!.Name.ToLower().Contains(search));
        }

        query = query.OrderByDescending(x => x.OrderDateUtc);

        return await query
            .Select(x => new SalesOrderListItemDto(
                x.Id,
                x.Number,
                x.BranchId,
                x.Branch!.Name,
                x.CustomerId,
                x.Customer!.Name,
                x.OrderDateUtc,
                x.Status,
                x.TotalAmount))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<SalesOrderDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.View);

        var entity = await _dbContext.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales order was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);

        return new SalesOrderDto(
            entity.Id,
            entity.Number,
            entity.BranchId,
            entity.Branch!.Name,
            entity.CustomerId,
            entity.Customer!.Name,
            entity.OrderDateUtc,
            entity.DueDateUtc,
            entity.Status,
            entity.TotalAmount,
            entity.Notes,
            entity.Lines.Select(x => new SalesOrderLineDto(
                x.Id,
                x.ProductId,
                x.Product!.Code,
                x.Product.Name,
                x.OrderedQuantity,
                x.DeliveredQuantity,
                x.UnitPrice,
                x.DiscountPercent,
                x.TaxPercent,
                x.LineTotal,
                x.Description)).ToList());
    }

    public async Task<Guid> CreateAsync(SaveSalesOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureReferencesAsync(request.BranchId, request.CustomerId, request.Lines.Select(x => x.ProductId).ToList(), cancellationToken);

        var number = await _numberSequenceService.NextAsync("SO", cancellationToken);
        var entity = new SalesOrder(number, request.CustomerId, request.BranchId, request.OrderDateUtc, request.DueDateUtc, request.Notes);
        entity.ReplaceLines(request.Lines.Select(x =>
            new SalesOrderLine(x.ProductId, x.Quantity, x.UnitPrice, x.DiscountPercent, x.TaxPercent, x.Description)));
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.SalesOrders.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(SalesOrder), entity.Id.ToString(), "Create", null, entity, entity.BranchId, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, SaveSalesOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureReferencesAsync(request.BranchId, request.CustomerId, request.Lines.Select(x => x.ProductId).ToList(), cancellationToken);

        var entity = await _dbContext.SalesOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales order was not found.");
        var before = await GetAsync(id, cancellationToken);

        entity.UpdateHeader(request.CustomerId, request.BranchId, request.OrderDateUtc, request.DueDateUtc, request.Notes);
        entity.ReplaceLines(request.Lines.Select(x =>
            new SalesOrderLine(x.ProductId, x.Quantity, x.UnitPrice, x.DiscountPercent, x.TaxPercent, x.Description)));
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(SalesOrder), entity.Id.ToString(), "Update", before, entity, entity.BranchId, cancellationToken);
    }

    public async Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.Manage);

        var entity = await _dbContext.SalesOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales order was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);

        entity.SubmitForApproval();
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        var result = await _approvalService.CreateRequestsAsync(
            ApprovalDocumentType.SalesOrder,
            entity.Id,
            entity.BranchId,
            entity.TotalAmount,
            cancellationToken);

        if (!result.HasApprovalRequests)
        {
            entity.Approve();
            await _auditService.LogAsync(nameof(SalesOrder), entity.Id.ToString(), "AutoApprove", null, entity, entity.BranchId, cancellationToken);
        }
        else
        {
            await _auditService.LogAsync(nameof(SalesOrder), entity.Id.ToString(), "SubmitForApproval", null, entity, entity.BranchId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.Manage);

        var entity = await _dbContext.SalesOrders.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales order was not found.");
        _currentUserService.EnsureBranchAccess(entity.BranchId);

        entity.Cancel();
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(SalesOrder), entity.Id.ToString(), "Cancel", null, entity, entity.BranchId, cancellationToken);
    }

    private async Task EnsureReferencesAsync(Guid branchId, Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == branchId && !x.IsDeleted, cancellationToken);
        if (!branchExists)
        {
            throw new NotFoundException("Branch was not found.");
        }

        var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer was not found.");
        }

        var availableProducts = await _dbContext.Products.CountAsync(x => productIds.Contains(x.Id) && !x.IsDeleted, cancellationToken);
        if (availableProducts != productIds.Distinct().Count())
        {
            throw new NotFoundException("One or more products were not found.");
        }
    }
}
