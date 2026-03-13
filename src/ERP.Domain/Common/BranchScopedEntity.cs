namespace ERP.Domain.Common;

public abstract class BranchScopedEntity : BaseEntity
{
    public Guid BranchId { get; protected set; }
}
