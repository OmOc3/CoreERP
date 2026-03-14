using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Application.Common.Contracts;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions AuditSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;

    public AuditService(IErpDbContext dbContext, ICurrentUserService currentUserService, IClock clock)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _clock = clock;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? before,
        object? after,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            BeforeData = before == null ? null : JsonSerializer.Serialize(before, AuditSerializerOptions),
            AfterData = after == null ? null : JsonSerializer.Serialize(after, AuditSerializerOptions),
            PerformedByUserId = _currentUserService.User.UserId,
            UserName = _currentUserService.User.UserName,
            BranchId = branchId,
            IpAddress = _currentUserService.User.IpAddress,
            TimestampUtc = _clock.UtcNow
        };
        log.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
