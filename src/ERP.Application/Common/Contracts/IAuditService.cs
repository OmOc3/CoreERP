namespace ERP.Application.Common.Contracts;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? before,
        object? after,
        Guid? branchId,
        CancellationToken cancellationToken = default);
}
