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

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetPagedAsync(PaymentQuery request, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
}

public sealed class PaymentQuery : BranchScopedQuery
{
    public PaymentType? Type { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? SupplierId { get; init; }
}

public sealed record PaymentDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    PaymentType Type,
    DateTime PaymentDateUtc,
    decimal Amount,
    string Method,
    string? ReferenceNumber,
    string? CounterpartyName,
    string? InvoiceNumber,
    PaymentStatus Status);

public sealed class CreatePaymentRequest
{
    public Guid BranchId { get; init; }
    public PaymentType Type { get; init; }
    public DateTime PaymentDateUtc { get; init; }
    public decimal Amount { get; init; }
    public string Method { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? SalesInvoiceId { get; init; }
    public Guid? PurchaseInvoiceId { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ReferenceNumber).MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(256);

        When(x => x.Type == PaymentType.CustomerReceipt, () =>
        {
            RuleFor(x => x.CustomerId).NotEmpty();
        });

        When(x => x.Type == PaymentType.SupplierPayment, () =>
        {
            RuleFor(x => x.SupplierId).NotEmpty();
        });
    }
}

public sealed class PaymentService : IPaymentService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IValidator<CreatePaymentRequest> _validator;

    public PaymentService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IValidator<CreatePaymentRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _validator = validator;
    }

    public async Task<PagedResult<PaymentDto>> GetPagedAsync(PaymentQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Payments.View);

        var query = _dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.SalesInvoice)
            .Include(x => x.PurchaseInvoice)
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

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.PaymentDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.PaymentDateUtc <= request.DateToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Number.ToLower().Contains(search) || (x.ReferenceNumber != null && x.ReferenceNumber.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(x => x.PaymentDateUtc)
            .Select(x => new PaymentDto(
                x.Id,
                x.Number,
                x.BranchId,
                x.Branch!.Name,
                x.Type,
                x.PaymentDateUtc,
                x.Amount,
                x.Method,
                x.ReferenceNumber,
                x.Type == PaymentType.CustomerReceipt ? x.Customer!.Name : x.Supplier!.Name,
                x.Type == PaymentType.CustomerReceipt ? x.SalesInvoice!.Number : x.PurchaseInvoice!.Number,
                x.Status))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Payments.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var number = await _numberSequenceService.NextAsync("PAY", cancellationToken);
        var entity = new Payment(
            number,
            request.BranchId,
            request.Type,
            request.PaymentDateUtc,
            request.Amount,
            request.Method,
            request.ReferenceNumber,
            request.CustomerId,
            request.SupplierId,
            request.SalesInvoiceId,
            request.PurchaseInvoiceId,
            request.Notes);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.Type == PaymentType.CustomerReceipt)
        {
            await ApplyCustomerPaymentAsync(request, cancellationToken);
        }
        else
        {
            await ApplySupplierPaymentAsync(request, cancellationToken);
        }

        _dbContext.Payments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _auditService.LogAsync(nameof(Payment), entity.Id.ToString(), "Create", null, entity, entity.BranchId, cancellationToken);

        return entity.Id;
    }

    private async Task ApplyCustomerPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId && !x.IsDeleted, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer was not found.");
        }

        if (!request.SalesInvoiceId.HasValue)
        {
            return;
        }

        var invoice = await _dbContext.SalesInvoices.SingleOrDefaultAsync(x => x.Id == request.SalesInvoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Sales invoice was not found.");

        if (invoice.CustomerId != request.CustomerId || invoice.BranchId != request.BranchId)
        {
            throw new ConflictException("Payment does not match the selected sales invoice.");
        }

        invoice.ApplyPayment(request.Amount);
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
    }

    private async Task ApplySupplierPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var supplierExists = await _dbContext.Suppliers.AnyAsync(x => x.Id == request.SupplierId && !x.IsDeleted, cancellationToken);
        if (!supplierExists)
        {
            throw new NotFoundException("Supplier was not found.");
        }

        if (!request.PurchaseInvoiceId.HasValue)
        {
            return;
        }

        var invoice = await _dbContext.PurchaseInvoices.SingleOrDefaultAsync(x => x.Id == request.PurchaseInvoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Purchase invoice was not found.");

        if (invoice.SupplierId != request.SupplierId || invoice.BranchId != request.BranchId)
        {
            throw new ConflictException("Payment does not match the selected purchase invoice.");
        }

        invoice.ApplyPayment(request.Amount);
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
    }
}
