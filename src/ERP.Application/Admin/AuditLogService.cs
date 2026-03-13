using ERP.Application.Common.Contracts;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Admin;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQuery request, CancellationToken cancellationToken);
}

public sealed class AuditLogQuery : BranchScopedQuery
{
    public string? EntityName { get; init; }
    public string? Action { get; init; }
}

public sealed record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string? UserName,
    Guid? BranchId,
    string? IpAddress,
    DateTime TimestampUtc,
    string? BeforeData,
    string? AfterData);

public sealed class AuditLogService : IAuditLogService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(IErpDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.AuditLogs.View);

        var query = _dbContext.AuditLogs.AsNoTracking().Where(x => !x.IsDeleted);

        if (request.BranchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(request.BranchId.Value);
            query = query.Where(x => x.BranchId == request.BranchId.Value);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            query = query.Where(x => !x.BranchId.HasValue || branchIds.Contains(x.BranchId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(x => x.EntityName == request.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(x => x.Action == request.Action);
        }

        if (request.DateFromUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc >= request.DateFromUtc.Value);
        }

        if (request.DateToUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc <= request.DateToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.EntityName.ToLower().Contains(search) ||
                x.EntityId.ToLower().Contains(search) ||
                (x.UserName != null && x.UserName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => new AuditLogDto(
                x.Id,
                x.EntityName,
                x.EntityId,
                x.Action,
                x.UserName,
                x.BranchId,
                x.IpAddress,
                x.TimestampUtc,
                x.BeforeData,
                x.AfterData))
            .ToPagedResultAsync(request, cancellationToken);
    }
}
