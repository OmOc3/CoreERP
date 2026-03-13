using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Dashboard;

public interface IAlertService
{
    Task<PagedResult<AlertDto>> GetPagedAsync(AlertQuery request, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AlertQuery : BranchScopedQuery
{
    public AlertType? Type { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public sealed record AlertDto(Guid Id, AlertType Type, Guid BranchId, string BranchName, string Title, string Message, bool IsRead, bool IsActive, DateTime TriggeredAtUtc);

public sealed class AlertService : IAlertService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AlertService(IErpDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<AlertDto>> GetPagedAsync(AlertQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Alerts.View);

        var query = _dbContext.Alerts
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => !x.IsDeleted);

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

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

        return await query
            .OrderByDescending(x => x.TriggeredAtUtc)
            .Select(x => new AlertDto(x.Id, x.Type, x.BranchId, x.Branch!.Name, x.Title, x.Message, x.IsRead, x.IsActive, x.TriggeredAtUtc))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Alerts.Manage);

        var entity = await _dbContext.Alerts.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Alert was not found.");

        _currentUserService.EnsureBranchAccess(entity.BranchId);
        entity.MarkRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
