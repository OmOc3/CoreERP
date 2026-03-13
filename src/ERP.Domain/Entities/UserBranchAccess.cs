using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class UserBranchAccess : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsDefault { get; set; }
}
