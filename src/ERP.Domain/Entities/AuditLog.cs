using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? UserName { get; set; }
    public Guid? BranchId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
