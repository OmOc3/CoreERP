using ERP.Application.Common.Models;

namespace ERP.Application.Common.Contracts;

public interface ICurrentUserService
{
    CurrentUserContext User { get; }
    Guid GetRequiredUserId();
    void EnsureAuthenticated();
    void EnsurePermission(string permission);
    void EnsureBranchAccess(Guid branchId);
}
