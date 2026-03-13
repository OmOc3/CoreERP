namespace ERP.Application.Common.Models;

public class BranchScopedQuery : ListQuery
{
    public Guid? BranchId { get; init; }
    public DateTime? DateFromUtc { get; init; }
    public DateTime? DateToUtc { get; init; }
}
