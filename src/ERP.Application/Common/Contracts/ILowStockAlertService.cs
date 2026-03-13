namespace ERP.Application.Common.Contracts;

public interface ILowStockAlertService
{
    Task<int> GenerateAsync(CancellationToken cancellationToken = default);
}
