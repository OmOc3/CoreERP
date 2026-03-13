using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Alert : BranchScopedEntity, IAggregateRoot
{
    private Alert()
    {
    }

    public Alert(AlertType type, Guid branchId, Guid? productId, string title, string message)
    {
        Type = type;
        BranchId = branchId;
        ProductId = productId;
        Title = title.Trim();
        Message = message.Trim();
        TriggeredAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public AlertType Type { get; private set; }
    public Guid? ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime TriggeredAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public void MarkRead()
    {
        IsRead = true;
    }

    public void Resolve()
    {
        IsActive = false;
        ResolvedAtUtc = DateTime.UtcNow;
    }
}
