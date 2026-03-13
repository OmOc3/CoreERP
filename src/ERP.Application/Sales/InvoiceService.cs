using ERP.Application.Approvals;
using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Application.Inventory;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Sales;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetPurchaseInvoicesAsync(InvoiceQuery request, CancellationToken cancellationToken);
    Task<PagedResult<InvoiceListItemDto>> GetSalesInvoicesAsync(InvoiceQuery request, CancellationToken cancellationToken);
    Task<InvoiceDetailDto> GetPurchaseInvoiceAsync(Guid id, CancellationToken cancellationToken);
    Task<InvoiceDetailDto> GetSalesInvoiceAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreatePurchaseInvoiceAsync(SavePurchaseInvoiceRequest request, CancellationToken cancellationToken);
    Task<Guid> CreateSalesInvoiceAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken);
}

public interface IInvoicePostingService
{
    Task PostPurchaseInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task PostSalesInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
}

public sealed class InvoiceQuery : BranchScopedQuery
{
    public Guid? CounterpartyId { get; init; }
    public InvoiceStatus? Status { get; init; }
}

public sealed record InvoiceListItemDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid CounterpartyId,
    string CounterpartyName,
    DateTime InvoiceDateUtc,
    DateTime DueDateUtc,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ReturnAmount,
    decimal OutstandingAmount);

public sealed record InvoiceLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal LineTotal);

public sealed record InvoiceDetailDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    Guid CounterpartyId,
    string CounterpartyName,
    DateTime InvoiceDateUtc,
    DateTime DueDateUtc,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ReturnAmount,
    decimal OutstandingAmount,
    string? Notes,
    IReadOnlyCollection<InvoiceLineDto> Lines);

public sealed class SavePurchaseInvoiceRequest
{
    public Guid BranchId { get; init; }
    public Guid SupplierId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public DateTime InvoiceDateUtc { get; init; }
    public DateTime DueDateUtc { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<SavePurchaseInvoiceLineRequest> Lines { get; init; } = Array.Empty<SavePurchaseInvoiceLineRequest>();
}

public sealed class SavePurchaseInvoiceLineRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxPercent { get; init; }
    public Guid? PurchaseOrderLineId { get; init; }
}

public sealed class SaveSalesInvoiceRequest
{
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? SalesOrderId { get; init; }
    public DateTime InvoiceDateUtc { get; init; }
    public DateTime DueDateUtc { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<SaveSalesInvoiceLineRequest> Lines { get; init; } = Array.Empty<SaveSalesInvoiceLineRequest>();
}

public sealed class SaveSalesInvoiceLineRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxPercent { get; init; }
    public Guid? SalesOrderLineId { get; init; }
}

public sealed class SavePurchaseInvoiceRequestValidator : AbstractValidator<SavePurchaseInvoiceRequest>
{
    public SavePurchaseInvoiceRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.InvoiceDateUtc).NotEmpty();
        RuleFor(x => x.DueDateUtc).GreaterThanOrEqualTo(x => x.InvoiceDateUtc);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new SavePurchaseInvoiceLineRequestValidator());
    }
}

public sealed class SavePurchaseInvoiceLineRequestValidator : AbstractValidator<SavePurchaseInvoiceLineRequest>
{
    public SavePurchaseInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveSalesInvoiceRequestValidator : AbstractValidator<SaveSalesInvoiceRequest>
{
    public SaveSalesInvoiceRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.InvoiceDateUtc).NotEmpty();
        RuleFor(x => x.DueDateUtc).GreaterThanOrEqualTo(x => x.InvoiceDateUtc);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new SaveSalesInvoiceLineRequestValidator());
    }
}

public sealed class SaveSalesInvoiceLineRequestValidator : AbstractValidator<SaveSalesInvoiceLineRequest>
{
    public SaveSalesInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).GreaterThanOrEqualTo(0);
    }
}

public sealed class InvoiceService : IInvoiceService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IApprovalService _approvalService;
    private readonly IInvoicePostingService _invoicePostingService;
    private readonly IValidator<SavePurchaseInvoiceRequest> _purchaseValidator;
    private readonly IValidator<SaveSalesInvoiceRequest> _salesValidator;

    public InvoiceService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IApprovalService approvalService,
        IInvoicePostingService invoicePostingService,
        IValidator<SavePurchaseInvoiceRequest> purchaseValidator,
        IValidator<SaveSalesInvoiceRequest> salesValidator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _approvalService = approvalService;
        _invoicePostingService = invoicePostingService;
        _purchaseValidator = purchaseValidator;
        _salesValidator = salesValidator;
    }

    public Task<PagedResult<InvoiceListItemDto>> GetPurchaseInvoicesAsync(InvoiceQuery request, CancellationToken cancellationToken)
        => GetPurchaseInvoicesInternalAsync(request, cancellationToken);

    public Task<PagedResult<InvoiceListItemDto>> GetSalesInvoicesAsync(InvoiceQuery request, CancellationToken cancellationToken)
        => GetSalesInvoicesInternalAsync(request, cancellationToken);

    public async Task<InvoiceDetailDto> GetPurchaseInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Invoices.View);

        var entity = await _dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Supplier)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase invoice was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);
        return MapPurchaseInvoice(entity);
    }

    public async Task<InvoiceDetailDto> GetSalesInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Invoices.View);

        var entity = await _dbContext.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales invoice was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);
        return MapSalesInvoice(entity);
    }

    public async Task<Guid> CreatePurchaseInvoiceAsync(SavePurchaseInvoiceRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.PurchaseOrders.Receive);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _purchaseValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsurePurchaseReferencesAsync(request, cancellationToken);

        var number = await _numberSequenceService.NextAsync("PINV", cancellationToken);
        var entity = new PurchaseInvoice(number, request.SupplierId, request.BranchId, request.PurchaseOrderId, request.InvoiceDateUtc, request.DueDateUtc, request.Notes);
        entity.ReplaceLines(request.Lines.Select(x => new PurchaseInvoiceLine(x.ProductId, x.Quantity, x.UnitPrice, x.TaxPercent, x.PurchaseOrderLineId)));
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.PurchaseInvoices.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var approval = await _approvalService.CreateRequestsAsync(
            ApprovalDocumentType.PurchaseInvoice,
            entity.Id,
            entity.BranchId,
            entity.TotalAmount,
            cancellationToken);

        if (approval.HasApprovalRequests)
        {
            entity.SubmitForApproval();
            entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(nameof(PurchaseInvoice), entity.Id.ToString(), "SubmitForApproval", null, entity, entity.BranchId, cancellationToken);
        }
        else
        {
            await _invoicePostingService.PostPurchaseInvoiceAsync(entity.Id, cancellationToken);
        }

        return entity.Id;
    }

    public async Task<Guid> CreateSalesInvoiceAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.SalesOrders.Invoice);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _salesValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureSalesReferencesAsync(request, cancellationToken);

        var number = await _numberSequenceService.NextAsync("SINV", cancellationToken);
        var entity = new SalesInvoice(number, request.CustomerId, request.BranchId, request.SalesOrderId, request.InvoiceDateUtc, request.DueDateUtc, request.Notes);
        entity.ReplaceLines(request.Lines.Select(x => new SalesInvoiceLine(x.ProductId, x.Quantity, x.UnitPrice, x.TaxPercent, x.SalesOrderLineId)));
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.SalesInvoices.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var approval = await _approvalService.CreateRequestsAsync(
            ApprovalDocumentType.SalesInvoice,
            entity.Id,
            entity.BranchId,
            entity.TotalAmount,
            cancellationToken);

        if (approval.HasApprovalRequests)
        {
            entity.SubmitForApproval();
            entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(nameof(SalesInvoice), entity.Id.ToString(), "SubmitForApproval", null, entity, entity.BranchId, cancellationToken);
        }
        else
        {
            await _invoicePostingService.PostSalesInvoiceAsync(entity.Id, cancellationToken);
        }

        return entity.Id;
    }

    private async Task<PagedResult<InvoiceListItemDto>> GetPurchaseInvoicesInternalAsync(InvoiceQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Invoices.View);

        var query = _dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted);

        query = ApplyInvoiceFilters(query, request);

        if (request.CounterpartyId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.CounterpartyId.Value);
        }

        return await query
            .OrderByDescending(x => x.InvoiceDateUtc)
            .Select(x => new InvoiceListItemDto(
                x.Id,
                x.Number,
                x.BranchId,
                x.Branch!.Name,
                x.SupplierId,
                x.Supplier!.Name,
                x.InvoiceDateUtc,
                x.DueDateUtc,
                x.Status,
                x.TotalAmount,
                x.PaidAmount,
                x.ReturnAmount,
                x.OutstandingAmount))
            .ToPagedResultAsync(request, cancellationToken);
    }

    private async Task<PagedResult<InvoiceListItemDto>> GetSalesInvoicesInternalAsync(InvoiceQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Invoices.View);

        var query = _dbContext.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Where(x => !x.IsDeleted);

        query = ApplyInvoiceFilters(query, request);

        if (request.CounterpartyId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CounterpartyId.Value);
        }

        return await query
            .OrderByDescending(x => x.InvoiceDateUtc)
            .Select(x => new InvoiceListItemDto(
                x.Id,
                x.Number,
                x.BranchId,
                x.Branch!.Name,
                x.CustomerId,
                x.Customer!.Name,
                x.InvoiceDateUtc,
                x.DueDateUtc,
                x.Status,
                x.TotalAmount,
                x.PaidAmount,
                x.ReturnAmount,
                x.OutstandingAmount))
            .ToPagedResultAsync(request, cancellationToken);
    }

    private IQueryable<TInvoice> ApplyInvoiceFilters<TInvoice>(IQueryable<TInvoice> query, InvoiceQuery request)
        where TInvoice : BranchScopedEntity
    {
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

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => EF.Property<DateTime>(x, "InvoiceDateUtc") >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => EF.Property<DateTime>(x, "InvoiceDateUtc") <= request.DateToUtc.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => EF.Property<InvoiceStatus>(x, "Status") == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Property<string>(x, "Number").ToLower().Contains(search));
        }

        return query;
    }

    private static InvoiceDetailDto MapPurchaseInvoice(PurchaseInvoice entity) =>
        new(
            entity.Id,
            entity.Number,
            entity.BranchId,
            entity.Branch!.Name,
            entity.SupplierId,
            entity.Supplier!.Name,
            entity.InvoiceDateUtc,
            entity.DueDateUtc,
            entity.Status,
            entity.TotalAmount,
            entity.PaidAmount,
            entity.ReturnAmount,
            entity.OutstandingAmount,
            entity.Notes,
            entity.Lines.Select(x => new InvoiceLineDto(x.Id, x.ProductId, x.Product!.Code, x.Product.Name, x.Quantity, x.UnitPrice, x.TaxPercent, x.LineTotal)).ToList());

    private static InvoiceDetailDto MapSalesInvoice(SalesInvoice entity) =>
        new(
            entity.Id,
            entity.Number,
            entity.BranchId,
            entity.Branch!.Name,
            entity.CustomerId,
            entity.Customer!.Name,
            entity.InvoiceDateUtc,
            entity.DueDateUtc,
            entity.Status,
            entity.TotalAmount,
            entity.PaidAmount,
            entity.ReturnAmount,
            entity.OutstandingAmount,
            entity.Notes,
            entity.Lines.Select(x => new InvoiceLineDto(x.Id, x.ProductId, x.Product!.Code, x.Product.Name, x.Quantity, x.UnitPrice, x.TaxPercent, x.LineTotal)).ToList());

    private async Task EnsurePurchaseReferencesAsync(SavePurchaseInvoiceRequest request, CancellationToken cancellationToken)
    {
        var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && !x.IsDeleted, cancellationToken);
        var supplierExists = await _dbContext.Suppliers.AnyAsync(x => x.Id == request.SupplierId && !x.IsDeleted, cancellationToken);
        if (!branchExists || !supplierExists)
        {
            throw new NotFoundException("Branch or supplier was not found.");
        }

        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
        var productCount = await _dbContext.Products.CountAsync(x => productIds.Contains(x.Id) && !x.IsDeleted, cancellationToken);
        if (productCount != productIds.Count)
        {
            throw new NotFoundException("One or more products were not found.");
        }

        if (!request.PurchaseOrderId.HasValue)
        {
            return;
        }

        var purchaseOrder = await _dbContext.PurchaseOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == request.PurchaseOrderId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase order was not found.");

        if (purchaseOrder.BranchId != request.BranchId || purchaseOrder.SupplierId != request.SupplierId)
        {
            throw new ConflictException("Purchase invoice branch or supplier does not match the purchase order.");
        }

        if (purchaseOrder.Status is not PurchaseOrderStatus.Approved and not PurchaseOrderStatus.PartiallyReceived)
        {
            throw new ConflictException("Only approved purchase orders can be invoiced.");
        }

        foreach (var line in request.Lines)
        {
            if (!line.PurchaseOrderLineId.HasValue)
            {
                continue;
            }

            var poLine = purchaseOrder.Lines.SingleOrDefault(x => x.Id == line.PurchaseOrderLineId.Value)
                ?? throw new NotFoundException("Referenced purchase order line was not found.");
            var remaining = poLine.OrderedQuantity - poLine.ReceivedQuantity;
            if (line.Quantity > remaining)
            {
                throw new ConflictException("Invoice quantity exceeds the remaining receivable quantity on the purchase order.");
            }
        }
    }

    private async Task EnsureSalesReferencesAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && !x.IsDeleted, cancellationToken);
        var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId && !x.IsDeleted, cancellationToken);
        if (!branchExists || !customerExists)
        {
            throw new NotFoundException("Branch or customer was not found.");
        }

        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
        var productCount = await _dbContext.Products.CountAsync(x => productIds.Contains(x.Id) && !x.IsDeleted, cancellationToken);
        if (productCount != productIds.Count)
        {
            throw new NotFoundException("One or more products were not found.");
        }

        if (!request.SalesOrderId.HasValue)
        {
            return;
        }

        var salesOrder = await _dbContext.SalesOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == request.SalesOrderId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales order was not found.");

        if (salesOrder.BranchId != request.BranchId || salesOrder.CustomerId != request.CustomerId)
        {
            throw new ConflictException("Sales invoice branch or customer does not match the sales order.");
        }

        if (salesOrder.Status is not SalesOrderStatus.Approved and not SalesOrderStatus.PartiallyDelivered)
        {
            throw new ConflictException("Only approved sales orders can be invoiced.");
        }

        foreach (var line in request.Lines)
        {
            if (!line.SalesOrderLineId.HasValue)
            {
                continue;
            }

            var soLine = salesOrder.Lines.SingleOrDefault(x => x.Id == line.SalesOrderLineId.Value)
                ?? throw new NotFoundException("Referenced sales order line was not found.");
            var remaining = soLine.OrderedQuantity - soLine.DeliveredQuantity;
            if (line.Quantity > remaining)
            {
                throw new ConflictException("Invoice quantity exceeds the remaining deliverable quantity on the sales order.");
            }
        }
    }
}

public sealed class InvoicePostingService : IInvoicePostingService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    public InvoicePostingService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IInventoryTransactionService inventoryTransactionService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _inventoryTransactionService = inventoryTransactionService;
    }

    public async Task PostPurchaseInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.PurchaseInvoices
            .Include(x => x.Lines)
            .Include(x => x.PurchaseOrder)
            .ThenInclude(x => x!.Lines)
            .SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase invoice was not found.");

        _currentUserService.EnsureBranchAccess(invoice.BranchId);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        invoice.Post();
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        foreach (var line in invoice.Lines)
        {
            if (invoice.PurchaseOrder != null && line.PurchaseOrderLineId.HasValue)
            {
                invoice.PurchaseOrder.RegisterReceipt(line.PurchaseOrderLineId.Value, line.Quantity);
                invoice.PurchaseOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
            }

            await _inventoryTransactionService.ReceiveAsync(
                invoice.BranchId,
                line.ProductId,
                line.Quantity,
                line.UnitPrice,
                InventoryMovementType.PurchaseReceipt,
                invoice.Number,
                nameof(PurchaseInvoice),
                invoice.Id,
                invoice.Notes,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _auditService.LogAsync(nameof(PurchaseInvoice), invoice.Id.ToString(), "Post", null, invoice, invoice.BranchId, cancellationToken);
    }

    public async Task PostSalesInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.SalesInvoices
            .Include(x => x.Lines)
            .Include(x => x.SalesOrder)
            .ThenInclude(x => x!.Lines)
            .SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales invoice was not found.");

        _currentUserService.EnsureBranchAccess(invoice.BranchId);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        invoice.Post();
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        foreach (var line in invoice.Lines)
        {
            if (invoice.SalesOrder != null && line.SalesOrderLineId.HasValue)
            {
                invoice.SalesOrder.RegisterDelivery(line.SalesOrderLineId.Value, line.Quantity);
                invoice.SalesOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
            }

            await _inventoryTransactionService.IssueAsync(
                invoice.BranchId,
                line.ProductId,
                line.Quantity,
                InventoryMovementType.SaleIssue,
                invoice.Number,
                nameof(SalesInvoice),
                invoice.Id,
                invoice.Notes,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _auditService.LogAsync(nameof(SalesInvoice), invoice.Id.ToString(), "Post", null, invoice, invoice.BranchId, cancellationToken);
    }
}
