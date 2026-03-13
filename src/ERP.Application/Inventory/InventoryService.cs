using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Inventory;

public interface IInventoryService
{
    Task<PagedResult<StockBalanceDto>> GetStockBalancesAsync(StockBalanceQuery request, CancellationToken cancellationToken);
    Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(InventoryMovementQuery request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LowStockItemDto>> GetLowStockAsync(Guid? branchId, CancellationToken cancellationToken);
    Task AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken);
    Task TransferStockAsync(StockTransferRequest request, CancellationToken cancellationToken);
}

public interface IInventoryTransactionService
{
    Task ReceiveAsync(
        Guid branchId,
        Guid productId,
        decimal quantity,
        decimal unitCost,
        InventoryMovementType movementType,
        string referenceNumber,
        string referenceDocumentType,
        Guid referenceDocumentId,
        string? remarks,
        CancellationToken cancellationToken);

    Task IssueAsync(
        Guid branchId,
        Guid productId,
        decimal quantity,
        InventoryMovementType movementType,
        string referenceNumber,
        string referenceDocumentType,
        Guid referenceDocumentId,
        string? remarks,
        CancellationToken cancellationToken);
}

public sealed class StockBalanceQuery : BranchScopedQuery
{
    public bool LowStockOnly { get; init; }
}

public sealed class InventoryMovementQuery : BranchScopedQuery
{
    public Guid? ProductId { get; init; }
}

public sealed record StockBalanceDto(
    Guid BranchId,
    string BranchName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal QuantityOnHand,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal AverageCost,
    decimal StockValue,
    decimal ReorderLevel,
    bool IsLowStock);

public sealed record InventoryMovementDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    DateTime MovementDateUtc,
    InventoryMovementType Type,
    decimal Quantity,
    decimal UnitCost,
    decimal QuantityAfter,
    decimal AverageCostAfter,
    string ReferenceNumber,
    string? ReferenceDocumentType,
    Guid? ReferenceDocumentId,
    string? Remarks);

public sealed record LowStockItemDto(Guid BranchId, string BranchName, Guid ProductId, string ProductCode, string ProductName, decimal QuantityOnHand, decimal ReorderLevel);

public sealed class StockAdjustmentRequest
{
    public Guid BranchId { get; init; }
    public Guid ProductId { get; init; }
    public decimal QuantityDifference { get; init; }
    public decimal UnitCost { get; init; }
    public string? Remarks { get; init; }
}

public sealed class StockTransferRequest
{
    public Guid FromBranchId { get; init; }
    public Guid ToBranchId { get; init; }
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public string? Remarks { get; init; }
}

public sealed class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    public StockAdjustmentRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.QuantityDifference).NotEqual(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Remarks).MaximumLength(256);
    }
}

public sealed class StockTransferRequestValidator : AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.FromBranchId).NotEmpty();
        RuleFor(x => x.ToBranchId).NotEmpty().NotEqual(x => x.FromBranchId);
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(256);
    }
}

public sealed class InventoryService : IInventoryService, IInventoryTransactionService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IInventoryPolicy _inventoryPolicy;
    private readonly IValidator<StockAdjustmentRequest> _adjustmentValidator;
    private readonly IValidator<StockTransferRequest> _transferValidator;

    public InventoryService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        INumberSequenceService numberSequenceService,
        IInventoryPolicy inventoryPolicy,
        IValidator<StockAdjustmentRequest> adjustmentValidator,
        IValidator<StockTransferRequest> transferValidator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _numberSequenceService = numberSequenceService;
        _inventoryPolicy = inventoryPolicy;
        _adjustmentValidator = adjustmentValidator;
        _transferValidator = transferValidator;
    }

    public async Task<PagedResult<StockBalanceDto>> GetStockBalancesAsync(StockBalanceQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Inventory.View);

        var query = _dbContext.StockBalances
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Include(x => x.Product!.Category)
            .Include(x => x.Product!.UnitOfMeasure)
            .Where(x => !x.IsDeleted && !x.Product!.IsDeleted);

        if (request.BranchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(request.BranchId.Value);
            query = query.Where(x => x.BranchId == request.BranchId);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            query = query.Where(x => branchIds.Contains(x.BranchId));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Product!.Code.ToLower().Contains(search) ||
                x.Product.Name.ToLower().Contains(search));
        }

        if (request.LowStockOnly)
        {
            query = query.Where(x => x.QuantityOnHand <= x.Product!.ReorderLevel);
        }

        query = query.OrderBy(x => x.Product!.Name);

        var projected = query.Select(x => new StockBalanceDto(
            x.BranchId,
            x.Branch!.Name,
            x.ProductId,
            x.Product!.Code,
            x.Product.Name,
            x.QuantityOnHand,
            x.ReservedQuantity,
            x.AvailableQuantity,
            x.AverageCost,
            x.StockValue,
            x.Product.ReorderLevel,
            x.QuantityOnHand <= x.Product.ReorderLevel));

        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(InventoryMovementQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Inventory.View);

        var query = _dbContext.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
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

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.MovementDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.MovementDateUtc <= request.DateToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.ReferenceNumber.ToLower().Contains(search) ||
                x.Product!.Code.ToLower().Contains(search) ||
                x.Product.Name.ToLower().Contains(search));
        }

        query = query.OrderByDescending(x => x.MovementDateUtc);

        return await query
            .Select(x => new InventoryMovementDto(
                x.Id,
                x.BranchId,
                x.Branch!.Name,
                x.ProductId,
                x.Product!.Code,
                x.Product.Name,
                x.MovementDateUtc,
                x.Type,
                x.Quantity,
                x.UnitCost,
                x.QuantityAfter,
                x.AverageCostAfter,
                x.ReferenceNumber,
                x.ReferenceDocumentType,
                x.ReferenceDocumentId,
                x.Remarks))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LowStockItemDto>> GetLowStockAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Alerts.View);

        var query = _dbContext.StockBalances
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Where(x => !x.IsDeleted && x.QuantityOnHand <= x.Product!.ReorderLevel);

        if (branchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(branchId.Value);
            query = query.Where(x => x.BranchId == branchId.Value);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            query = query.Where(x => branchIds.Contains(x.BranchId));
        }

        return await query
            .OrderBy(x => x.Product!.Name)
            .Select(x => new LowStockItemDto(
                x.BranchId,
                x.Branch!.Name,
                x.ProductId,
                x.Product!.Code,
                x.Product.Name,
                x.QuantityOnHand,
                x.Product.ReorderLevel))
            .ToListAsync(cancellationToken);
    }

    public async Task AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Inventory.Adjust);
        _currentUserService.EnsureBranchAccess(request.BranchId);
        await _adjustmentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var referenceNumber = await _numberSequenceService.NextAsync("ADJ", cancellationToken);
        if (request.QuantityDifference > 0)
        {
            await ReceiveAsync(
                request.BranchId,
                request.ProductId,
                request.QuantityDifference,
                request.UnitCost,
                InventoryMovementType.StockAdjustmentIncrease,
                referenceNumber,
                nameof(StockAdjustmentRequest),
                Guid.Empty,
                request.Remarks,
                cancellationToken);
        }
        else
        {
            await IssueAsync(
                request.BranchId,
                request.ProductId,
                Math.Abs(request.QuantityDifference),
                InventoryMovementType.StockAdjustmentDecrease,
                referenceNumber,
                nameof(StockAdjustmentRequest),
                Guid.Empty,
                request.Remarks,
                cancellationToken);
        }

        await _auditService.LogAsync("StockAdjustment", referenceNumber, "Post", null, request, request.BranchId, cancellationToken);
    }

    public async Task TransferStockAsync(StockTransferRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Inventory.Transfer);
        _currentUserService.EnsureBranchAccess(request.FromBranchId);
        _currentUserService.EnsureBranchAccess(request.ToBranchId);
        await _transferValidator.ValidateAndThrowAsync(request, cancellationToken);

        var stock = await GetOrCreateStockBalanceAsync(request.FromBranchId, request.ProductId, cancellationToken);
        var transferNumber = await _numberSequenceService.NextAsync("TRF", cancellationToken);
        var unitCost = stock.AverageCost;

        await IssueAsync(
            request.FromBranchId,
            request.ProductId,
            request.Quantity,
            InventoryMovementType.TransferOut,
            transferNumber,
            nameof(StockTransferRequest),
            Guid.Empty,
            request.Remarks,
            cancellationToken);

        await ReceiveAsync(
            request.ToBranchId,
            request.ProductId,
            request.Quantity,
            unitCost,
            InventoryMovementType.TransferIn,
            transferNumber,
            nameof(StockTransferRequest),
            Guid.Empty,
            request.Remarks,
            cancellationToken);

        await _auditService.LogAsync("StockTransfer", transferNumber, "Post", null, request, request.FromBranchId, cancellationToken);
    }

    public async Task ReceiveAsync(
        Guid branchId,
        Guid productId,
        decimal quantity,
        decimal unitCost,
        InventoryMovementType movementType,
        string referenceNumber,
        string referenceDocumentType,
        Guid referenceDocumentId,
        string? remarks,
        CancellationToken cancellationToken)
    {
        var stock = await GetOrCreateStockBalanceAsync(branchId, productId, cancellationToken);
        stock.Receive(quantity, unitCost);
        stock.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.InventoryMovements.Add(new InventoryMovement(
            branchId,
            productId,
            _clock.UtcNow,
            movementType,
            quantity,
            unitCost,
            stock.QuantityOnHand,
            stock.AverageCost,
            referenceNumber,
            referenceDocumentType,
            referenceDocumentId == Guid.Empty ? null : referenceDocumentId,
            remarks));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task IssueAsync(
        Guid branchId,
        Guid productId,
        decimal quantity,
        InventoryMovementType movementType,
        string referenceNumber,
        string referenceDocumentType,
        Guid referenceDocumentId,
        string? remarks,
        CancellationToken cancellationToken)
    {
        var stock = await GetOrCreateStockBalanceAsync(branchId, productId, cancellationToken);
        stock.Issue(quantity, _inventoryPolicy.AllowNegativeStock);
        stock.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.InventoryMovements.Add(new InventoryMovement(
            branchId,
            productId,
            _clock.UtcNow,
            movementType,
            -quantity,
            stock.AverageCost,
            stock.QuantityOnHand,
            stock.AverageCost,
            referenceNumber,
            referenceDocumentType,
            referenceDocumentId == Guid.Empty ? null : referenceDocumentId,
            remarks));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<StockBalance> GetOrCreateStockBalanceAsync(Guid branchId, Guid productId, CancellationToken cancellationToken)
    {
        var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == branchId && !x.IsDeleted, cancellationToken);
        if (!branchExists)
        {
            throw new NotFoundException("Branch was not found.");
        }

        var productExists = await _dbContext.Products.AnyAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken);
        if (!productExists)
        {
            throw new NotFoundException("Product was not found.");
        }

        var stockBalance = await _dbContext.StockBalances.SingleOrDefaultAsync(
            x => x.BranchId == branchId && x.ProductId == productId && !x.IsDeleted,
            cancellationToken);

        if (stockBalance != null)
        {
            return stockBalance;
        }

        stockBalance = new StockBalance(branchId, productId);
        stockBalance.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);
        _dbContext.StockBalances.Add(stockBalance);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stockBalance;
    }
}
