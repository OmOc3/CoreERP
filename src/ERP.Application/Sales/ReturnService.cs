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

public interface IReturnService
{
    Task<PagedResult<ReturnListItemDto>> GetPagedAsync(ReturnQuery request, CancellationToken cancellationToken);
    Task<ReturnDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreateReturnRequest request, CancellationToken cancellationToken);
}

public sealed class ReturnQuery : BranchScopedQuery
{
    public ReturnDocumentType? Type { get; init; }
}

public sealed record ReturnListItemDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    ReturnDocumentType Type,
    DateTime ReturnDateUtc,
    ReturnStatus Status,
    decimal TotalAmount);

public sealed record ReturnLineDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Reason);

public sealed record ReturnDto(
    Guid Id,
    string Number,
    Guid BranchId,
    string BranchName,
    ReturnDocumentType Type,
    DateTime ReturnDateUtc,
    ReturnStatus Status,
    decimal TotalAmount,
    string? Reason,
    Guid? SalesInvoiceId,
    Guid? PurchaseInvoiceId,
    IReadOnlyCollection<ReturnLineDto> Lines);

public sealed class CreateReturnRequest
{
    public Guid BranchId { get; init; }
    public ReturnDocumentType Type { get; init; }
    public DateTime ReturnDateUtc { get; init; }
    public Guid? SalesInvoiceId { get; init; }
    public Guid? PurchaseInvoiceId { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyCollection<CreateReturnLineRequest> Lines { get; init; } = Array.Empty<CreateReturnLineRequest>();
}

public sealed class CreateReturnLineRequest
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string? Reason { get; init; }
}

public sealed class CreateReturnRequestValidator : AbstractValidator<CreateReturnRequest>
{
    public CreateReturnRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ReturnDateUtc).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new CreateReturnLineRequestValidator());
        RuleFor(x => x.SalesInvoiceId).NotEmpty().When(x => x.Type == ReturnDocumentType.SalesReturn);
        RuleFor(x => x.PurchaseInvoiceId).NotEmpty().When(x => x.Type == ReturnDocumentType.PurchaseReturn);
    }
}

public sealed class CreateReturnLineRequestValidator : AbstractValidator<CreateReturnLineRequest>
{
    public CreateReturnLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).MaximumLength(256);
    }
}

public sealed class ReturnService : IReturnService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IInventoryTransactionService _inventoryTransactionService;
    private readonly IValidator<CreateReturnRequest> _validator;

    public ReturnService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IInventoryTransactionService inventoryTransactionService,
        IValidator<CreateReturnRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _inventoryTransactionService = inventoryTransactionService;
        _validator = validator;
    }

    public async Task<PagedResult<ReturnListItemDto>> GetPagedAsync(ReturnQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Returns.View);

        var query = _dbContext.ReturnDocuments
            .AsNoTracking()
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

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.ReturnDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.ReturnDateUtc <= request.DateToUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.ReturnDateUtc)
            .Select(x => new ReturnListItemDto(x.Id, x.Number, x.BranchId, x.Branch!.Name, x.Type, x.ReturnDateUtc, x.Status, x.TotalAmount))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ReturnDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Returns.View);

        var entity = await _dbContext.ReturnDocuments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Return document was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);

        return new ReturnDto(
            entity.Id,
            entity.Number,
            entity.BranchId,
            entity.Branch!.Name,
            entity.Type,
            entity.ReturnDateUtc,
            entity.Status,
            entity.TotalAmount,
            entity.Reason,
            entity.SalesInvoiceId,
            entity.PurchaseInvoiceId,
            entity.Lines.Select(x => new ReturnLineDto(x.Id, x.ProductId, x.Product!.Code, x.Product.Name, x.Quantity, x.UnitPrice, x.LineTotal, x.Reason)).ToList());
    }

    public async Task<Guid> CreateAsync(CreateReturnRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Returns.Manage);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var number = await _numberSequenceService.NextAsync("RET", cancellationToken);
        var entity = new ReturnDocument(
            number,
            request.Type,
            request.BranchId,
            request.ReturnDateUtc,
            null,
            null,
            request.SalesInvoiceId,
            request.PurchaseInvoiceId,
            request.Reason);
        entity.ReplaceLines(request.Lines.Select(x => new ReturnLine(x.ProductId, x.Quantity, x.UnitPrice, x.Reason)));
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.ReturnDocuments.Add(entity);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.Type == ReturnDocumentType.SalesReturn)
        {
            await ProcessSalesReturnAsync(entity, request, cancellationToken);
        }
        else
        {
            await ProcessPurchaseReturnAsync(entity, request, cancellationToken);
        }

        entity.Post();
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ReturnDocument), entity.Id.ToString(), "Post", null, entity, entity.BranchId, cancellationToken);
        return entity.Id;
    }

    private async Task ProcessSalesReturnAsync(ReturnDocument entity, CreateReturnRequest request, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.SalesInvoices.SingleOrDefaultAsync(x => x.Id == request.SalesInvoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Referenced sales invoice was not found.");

        if (invoice.BranchId != request.BranchId)
        {
            throw new ConflictException("Return branch must match the original sales invoice branch.");
        }

        foreach (var line in entity.Lines)
        {
            var unitCost = await _dbContext.StockBalances
                .Where(x => x.BranchId == entity.BranchId && x.ProductId == line.ProductId && !x.IsDeleted)
                .Select(x => (decimal?)x.AverageCost)
                .SingleOrDefaultAsync(cancellationToken)
                ?? await _dbContext.Products
                    .Where(x => x.Id == line.ProductId && !x.IsDeleted)
                    .Select(x => x.StandardCost)
                    .SingleAsync(cancellationToken);

            await _inventoryTransactionService.ReceiveAsync(
                entity.BranchId,
                line.ProductId,
                line.Quantity,
                unitCost,
                InventoryMovementType.SalesReturn,
                entity.Number,
                nameof(ReturnDocument),
                entity.Id,
                entity.Reason,
                cancellationToken);
        }

        invoice.ApplyReturn(entity.TotalAmount);
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
    }

    private async Task ProcessPurchaseReturnAsync(ReturnDocument entity, CreateReturnRequest request, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.PurchaseInvoices.SingleOrDefaultAsync(x => x.Id == request.PurchaseInvoiceId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Referenced purchase invoice was not found.");

        if (invoice.BranchId != request.BranchId)
        {
            throw new ConflictException("Return branch must match the original purchase invoice branch.");
        }

        foreach (var line in entity.Lines)
        {
            await _inventoryTransactionService.IssueAsync(
                entity.BranchId,
                line.ProductId,
                line.Quantity,
                InventoryMovementType.PurchaseReturn,
                entity.Number,
                nameof(ReturnDocument),
                entity.Id,
                entity.Reason,
                cancellationToken);
        }

        invoice.ApplyReturn(entity.TotalAmount);
        invoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
    }
}
