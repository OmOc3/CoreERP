using ERP.Application.Common.Contracts;
using ERP.Application.Common.Security;
using ERP.Domain.Common;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(Guid? branchId, DateTime? dateFromUtc, DateTime? dateToUtc, CancellationToken cancellationToken);
}

public sealed record KpiCardDto(string Key, string Label, decimal Value);
public sealed record TrendPointDto(DateTime Date, decimal Sales, decimal Purchases);
public sealed record DashboardItemDto(string Label, decimal Value);
public sealed record DashboardAlertDto(Guid Id, string Title, string Message, DateTime TriggeredAtUtc, bool IsRead);

public sealed record DashboardDto(
    IReadOnlyCollection<KpiCardDto> Kpis,
    IReadOnlyCollection<TrendPointDto> Trends,
    IReadOnlyCollection<DashboardItemDto> TopProducts,
    IReadOnlyCollection<DashboardItemDto> TopCustomers,
    IReadOnlyCollection<DashboardAlertDto> LowStockAlerts,
    int PendingApprovals,
    int LowStockCount,
    decimal PaidInvoices,
    decimal OpenInvoices);

public sealed class DashboardService : IDashboardService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;

    public DashboardService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _clock = clock;
    }

    public async Task<DashboardDto> GetAsync(Guid? branchId, DateTime? dateFromUtc, DateTime? dateToUtc, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Dashboard.View);

        var from = dateFromUtc ?? _clock.UtcNow.Date.AddDays(-30);
        var to = dateToUtc ?? _clock.UtcNow.Date;
        if (branchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(branchId.Value);
        }

        var salesInvoices = ApplyBranchScope(_dbContext.SalesInvoices.AsNoTracking().Where(x => !x.IsDeleted), branchId)
            .Where(x => x.InvoiceDateUtc >= from && x.InvoiceDateUtc <= to && x.Status != InvoiceStatus.Draft && x.Status != InvoiceStatus.Rejected);
        var purchaseInvoices = ApplyBranchScope(_dbContext.PurchaseInvoices.AsNoTracking().Where(x => !x.IsDeleted), branchId)
            .Where(x => x.InvoiceDateUtc >= from && x.InvoiceDateUtc <= to && x.Status != InvoiceStatus.Draft && x.Status != InvoiceStatus.Rejected);
        var stockBalances = ApplyBranchScope(_dbContext.StockBalances.AsNoTracking().Include(x => x.Product).Where(x => !x.IsDeleted), branchId);
        var approvals = ApplyApprovalBranchScope(_dbContext.ApprovalRequests.AsNoTracking().Where(x => !x.IsDeleted), branchId);
        var alerts = ApplyBranchScope(_dbContext.Alerts.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive && x.Type == AlertType.LowStock), branchId);

        var totalSales = await salesInvoices.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;
        var totalPurchases = await purchaseInvoices.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;
        var netRevenue = totalSales - totalPurchases;
        var stockValue = await stockBalances.SumAsync(x => (decimal?)x.StockValue, cancellationToken) ?? 0m;
        var pendingApprovals = await approvals.CountAsync(x => x.Status == ApprovalStatus.Pending, cancellationToken);
        var lowStockCount = await stockBalances.CountAsync(x => x.QuantityOnHand <= x.Product!.ReorderLevel, cancellationToken);
        var paidInvoices = await salesInvoices.SumAsync(x => (decimal?)x.PaidAmount, cancellationToken) ?? 0m;
        var openInvoices = await salesInvoices.SumAsync(x => (decimal?)x.OutstandingAmount, cancellationToken) ?? 0m;

        var trends = await salesInvoices
            .GroupBy(x => x.InvoiceDateUtc.Date)
            .Select(x => new { Date = x.Key, Sales = x.Sum(y => y.TotalAmount) })
            .ToListAsync(cancellationToken);
        var purchaseTrends = await purchaseInvoices
            .GroupBy(x => x.InvoiceDateUtc.Date)
            .Select(x => new { Date = x.Key, Purchases = x.Sum(y => y.TotalAmount) })
            .ToListAsync(cancellationToken);

        var trendMap = trends.ToDictionary(x => x.Date, x => x.Sales);
        var purchaseMap = purchaseTrends.ToDictionary(x => x.Date, x => x.Purchases);
        var trendPoints = Enumerable.Range(0, (to.Date - from.Date).Days + 1)
            .Select(offset => from.Date.AddDays(offset))
            .Select(date => new TrendPointDto(
                date,
                trendMap.TryGetValue(date, out var sales) ? sales : 0m,
                purchaseMap.TryGetValue(date, out var purchases) ? purchases : 0m))
            .ToList();

        var topProductsQuery = _dbContext.SalesInvoiceLines
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.SalesInvoice)
            .Where(x => !x.IsDeleted && x.SalesInvoice!.InvoiceDateUtc >= from && x.SalesInvoice.InvoiceDateUtc <= to);
        if (branchId.HasValue)
        {
            topProductsQuery = topProductsQuery.Where(x => x.SalesInvoice!.BranchId == branchId.Value);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            topProductsQuery = topProductsQuery.Where(x => branchIds.Contains(x.SalesInvoice!.BranchId));
        }

        var topProducts = await topProductsQuery
            .GroupBy(x => new { x.ProductId, x.Product!.Name })
            .Select(x => new DashboardItemDto(x.Key.Name, x.Sum(y => y.Quantity)))
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topCustomers = await ApplyBranchScope(_dbContext.SalesInvoices.AsNoTracking().Include(x => x.Customer), branchId)
            .Where(x => !x.IsDeleted && x.InvoiceDateUtc >= from && x.InvoiceDateUtc <= to)
            .GroupBy(x => new { x.CustomerId, x.Customer!.Name })
            .Select(x => new DashboardItemDto(x.Key.Name, x.Sum(y => y.TotalAmount)))
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        var lowStockAlerts = await alerts
            .OrderByDescending(x => x.TriggeredAtUtc)
            .Take(10)
            .Select(x => new DashboardAlertDto(x.Id, x.Title, x.Message, x.TriggeredAtUtc, x.IsRead))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
        [
            new("totalSales", "Total Sales", totalSales),
            new("totalPurchases", "Total Purchases", totalPurchases),
            new("netRevenue", "Net Revenue", netRevenue),
            new("stockValue", "Stock Value", stockValue)
        ],
            trendPoints,
            topProducts,
            topCustomers,
            lowStockAlerts,
            pendingApprovals,
            lowStockCount,
            paidInvoices,
            openInvoices);
    }

    private IQueryable<TBranchScoped> ApplyBranchScope<TBranchScoped>(IQueryable<TBranchScoped> query, Guid? branchId)
        where TBranchScoped : BranchScopedEntity
    {
        if (branchId.HasValue)
        {
            return query.Where(x => x.BranchId == branchId.Value);
        }

        if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            return query.Where(x => branchIds.Contains(x.BranchId));
        }

        return query;
    }

    private IQueryable<ApprovalRequest> ApplyApprovalBranchScope(IQueryable<ApprovalRequest> query, Guid? branchId)
    {
        if (branchId.HasValue)
        {
            return query.Where(x => x.BranchId == branchId.Value);
        }

        if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            return query.Where(x => !x.BranchId.HasValue || branchIds.Contains(x.BranchId.Value));
        }

        return query;
    }
}
