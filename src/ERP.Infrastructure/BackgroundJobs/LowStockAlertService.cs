using ERP.Application.Common.Contracts;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.BackgroundJobs;

public sealed class LowStockAlertService : ILowStockAlertService
{
    private readonly IErpDbContext _dbContext;
    private readonly IClock _clock;

    public LowStockAlertService(IErpDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<int> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var lowStockItems = await _dbContext.StockBalances
            .Include(x => x.Branch)
            .Include(x => x.Product)
            .Where(x => !x.IsDeleted && x.QuantityOnHand <= x.Product!.ReorderLevel)
            .ToListAsync(cancellationToken);

        var existingAlerts = await _dbContext.Alerts
            .Where(x => !x.IsDeleted && x.Type == AlertType.LowStock)
            .ToListAsync(cancellationToken);

        foreach (var existingAlert in existingAlerts)
        {
            var stillLow = lowStockItems.Any(x => x.BranchId == existingAlert.BranchId && x.ProductId == existingAlert.ProductId);
            if (!stillLow && existingAlert.IsActive)
            {
                existingAlert.Resolve();
            }
        }

        foreach (var item in lowStockItems)
        {
            var alert = existingAlerts.SingleOrDefault(x => x.BranchId == item.BranchId && x.ProductId == item.ProductId);
            if (alert == null)
            {
                alert = new Alert(
                    AlertType.LowStock,
                    item.BranchId,
                    item.ProductId,
                    $"Low stock for {item.Product!.Name}",
                    $"{item.Branch!.Name}: available quantity {item.QuantityOnHand} is below reorder level {item.Product.ReorderLevel}");
                alert.SetCreationAudit(_clock.UtcNow, "system");
                _dbContext.Alerts.Add(alert);
                continue;
            }

            if (!alert.IsActive)
            {
                alert = new Alert(
                    AlertType.LowStock,
                    item.BranchId,
                    item.ProductId,
                    $"Low stock for {item.Product!.Name}",
                    $"{item.Branch!.Name}: available quantity {item.QuantityOnHand} is below reorder level {item.Product.ReorderLevel}");
                alert.SetCreationAudit(_clock.UtcNow, "system");
                _dbContext.Alerts.Add(alert);
            }
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
