using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyCollection<SalesSummaryRowDto>> GetSalesSummaryAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PurchaseSummaryRowDto>> GetPurchaseSummaryAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InventoryValuationRowDto>> GetInventoryValuationAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StockMovementRowDto>> GetStockMovementAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LowStockReportRowDto>> GetLowStockAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ReceivableRowDto>> GetReceivablesAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PayableRowDto>> GetPayablesAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportSalesSummaryExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportSalesSummaryPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportPurchaseSummaryExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportPurchaseSummaryPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportInventoryValuationExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportInventoryValuationPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportStockMovementExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportStockMovementPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportLowStockExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportLowStockPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportReceivablesExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportReceivablesPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportPayablesExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken);
    Task<ReportFileResult> ExportPayablesPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken);
}

public sealed class ReportFilterRequest
{
    public Guid? BranchId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? ProductId { get; init; }
    public DateTime? DateFromUtc { get; init; }
    public DateTime? DateToUtc { get; init; }
}

public sealed record SalesSummaryRowDto(DateTime InvoiceDateUtc, string BranchName, string CustomerName, string InvoiceNumber, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount);
public sealed record PurchaseSummaryRowDto(DateTime InvoiceDateUtc, string BranchName, string SupplierName, string InvoiceNumber, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount);
public sealed record InventoryValuationRowDto(string BranchName, string ProductCode, string ProductName, decimal QuantityOnHand, decimal AverageCost, decimal StockValue);
public sealed record StockMovementRowDto(DateTime MovementDateUtc, string BranchName, string ProductCode, string ProductName, string MovementType, decimal Quantity, decimal UnitCost, string ReferenceNumber);
public sealed record LowStockReportRowDto(string BranchName, string ProductCode, string ProductName, decimal QuantityOnHand, decimal ReorderLevel);
public sealed record ReceivableRowDto(string BranchName, string CustomerName, string InvoiceNumber, DateTime DueDateUtc, decimal OutstandingAmount, int DaysOverdue);
public sealed record PayableRowDto(string BranchName, string SupplierName, string InvoiceNumber, DateTime DueDateUtc, decimal OutstandingAmount, int DaysOverdue);
public sealed record ReportFileResult(string FileName, string ContentType, byte[] Content);

public sealed class ReportService : IReportService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IReportExportService _reportExportService;
    private readonly IClock _clock;

    public ReportService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IReportExportService reportExportService,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _reportExportService = reportExportService;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<SalesSummaryRowDto>> GetSalesSummaryAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var query = _dbContext.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Where(x => !x.IsDeleted && x.Status != InvoiceStatus.Draft && x.Status != InvoiceStatus.Rejected);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.InvoiceDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.InvoiceDateUtc <= request.DateToUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.InvoiceDateUtc)
            .Select(x => new SalesSummaryRowDto(x.InvoiceDateUtc, x.Branch!.Name, x.Customer!.Name, x.Number, x.TotalAmount, x.PaidAmount, x.OutstandingAmount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PurchaseSummaryRowDto>> GetPurchaseSummaryAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var query = _dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted && x.Status != InvoiceStatus.Draft && x.Status != InvoiceStatus.Rejected);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.SupplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.InvoiceDateUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.InvoiceDateUtc <= request.DateToUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.InvoiceDateUtc)
            .Select(x => new PurchaseSummaryRowDto(x.InvoiceDateUtc, x.Branch!.Name, x.Supplier!.Name, x.Number, x.TotalAmount, x.PaidAmount, x.OutstandingAmount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<InventoryValuationRowDto>> GetInventoryValuationAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var query = _dbContext.StockBalances
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Where(x => !x.IsDeleted);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        return await query
            .OrderBy(x => x.Product!.Name)
            .Select(x => new InventoryValuationRowDto(x.Branch!.Name, x.Product!.Code, x.Product.Name, x.QuantityOnHand, x.AverageCost, x.StockValue))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockMovementRowDto>> GetStockMovementAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var query = _dbContext.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Where(x => !x.IsDeleted);

        query = ApplyBranchFilter(query, request.BranchId);

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

        return await query
            .OrderByDescending(x => x.MovementDateUtc)
            .Select(x => new StockMovementRowDto(x.MovementDateUtc, x.Branch!.Name, x.Product!.Code, x.Product.Name, x.Type.ToString(), x.Quantity, x.UnitCost, x.ReferenceNumber))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LowStockReportRowDto>> GetLowStockAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var query = _dbContext.StockBalances
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Where(x => !x.IsDeleted && x.QuantityOnHand <= x.Product!.ReorderLevel);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        return await query
            .OrderBy(x => x.Product!.Name)
            .Select(x => new LowStockReportRowDto(x.Branch!.Name, x.Product!.Code, x.Product.Name, x.QuantityOnHand, x.Product.ReorderLevel))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReceivableRowDto>> GetReceivablesAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var today = _clock.UtcNow.Date;
        var query = _dbContext.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Where(x => !x.IsDeleted && x.TotalAmount - x.PaidAmount - x.ReturnAmount > 0);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        var items = await query
            .OrderBy(x => x.DueDateUtc)
            .Select(x => new ReceivableRowDto(
                x.Branch!.Name,
                x.Customer!.Name,
                x.Number,
                x.DueDateUtc,
                x.TotalAmount - x.PaidAmount - x.ReturnAmount,
                0))
            .ToListAsync(cancellationToken);

        return items
            .Select(x => x with { DaysOverdue = (today - x.DueDateUtc.Date).Days })
            .ToList();
    }

    public async Task<IReadOnlyCollection<PayableRowDto>> GetPayablesAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Reports.View);

        var today = _clock.UtcNow.Date;
        var query = _dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted && x.TotalAmount - x.PaidAmount - x.ReturnAmount > 0);

        query = ApplyBranchFilter(query, request.BranchId);

        if (request.SupplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);
        }

        var items = await query
            .OrderBy(x => x.DueDateUtc)
            .Select(x => new PayableRowDto(
                x.Branch!.Name,
                x.Supplier!.Name,
                x.Number,
                x.DueDateUtc,
                x.TotalAmount - x.PaidAmount - x.ReturnAmount,
                0))
            .ToListAsync(cancellationToken);

        return items
            .Select(x => x with { DaysOverdue = (today - x.DueDateUtc.Date).Days })
            .ToList();
    }

    public async Task<ReportFileResult> ExportSalesSummaryExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("sales-summary.xlsx", "SalesSummary", await GetSalesSummaryAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportSalesSummaryPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("sales-summary.pdf", "Sales Summary", await GetSalesSummaryAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportPurchaseSummaryExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("purchase-summary.xlsx", "PurchaseSummary", await GetPurchaseSummaryAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportPurchaseSummaryPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("purchase-summary.pdf", "Purchase Summary", await GetPurchaseSummaryAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportInventoryValuationExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("inventory-valuation.xlsx", "InventoryValuation", await GetInventoryValuationAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportInventoryValuationPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("inventory-valuation.pdf", "Inventory Valuation", await GetInventoryValuationAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportStockMovementExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("stock-movement.xlsx", "StockMovement", await GetStockMovementAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportStockMovementPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("stock-movement.pdf", "Stock Movement", await GetStockMovementAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportLowStockExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("low-stock.xlsx", "LowStock", await GetLowStockAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportLowStockPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("low-stock.pdf", "Low Stock", await GetLowStockAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportReceivablesExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("receivables.xlsx", "Receivables", await GetReceivablesAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportReceivablesPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("receivables.pdf", "Receivables", await GetReceivablesAsync(request, cancellationToken), request);

    public async Task<ReportFileResult> ExportPayablesExcelAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildExcel("payables.xlsx", "Payables", await GetPayablesAsync(request, cancellationToken));

    public async Task<ReportFileResult> ExportPayablesPdfAsync(ReportFilterRequest request, CancellationToken cancellationToken)
        => BuildPdf("payables.pdf", "Payables", await GetPayablesAsync(request, cancellationToken), request);

    private IQueryable<TBranchScoped> ApplyBranchFilter<TBranchScoped>(IQueryable<TBranchScoped> query, Guid? branchId)
        where TBranchScoped : BranchScopedEntity
    {
        if (branchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(branchId.Value);
            return query.Where(x => x.BranchId == branchId.Value);
        }

        if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            return query.Where(x => branchIds.Contains(x.BranchId));
        }

        return query;
    }

    private ReportFileResult BuildExcel<T>(string fileName, string worksheetName, IReadOnlyCollection<T> rows)
    {
        var bytes = _reportExportService.ExportToExcel(worksheetName, rows);
        return new ReportFileResult(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }

    private ReportFileResult BuildPdf<T>(string fileName, string title, IReadOnlyCollection<T> rows, ReportFilterRequest filter)
    {
        var filters = new List<KeyValuePair<string, string>>
        {
            new("Branch", filter.BranchId?.ToString() ?? "All"),
            new("From", filter.DateFromUtc?.ToString("yyyy-MM-dd") ?? "Any"),
            new("To", filter.DateToUtc?.ToString("yyyy-MM-dd") ?? "Any")
        };

        var properties = typeof(T).GetProperties();
        var columns = properties.Select(x => x.Name).ToList();
        var data = rows
            .Select(row => (IReadOnlyCollection<string>)properties.Select(p => Convert.ToString(p.GetValue(row)) ?? string.Empty).ToList())
            .ToList();

        var bytes = _reportExportService.ExportToPdf(title, filters, columns, data);
        return new ReportFileResult(fileName, "application/pdf", bytes);
    }
}
