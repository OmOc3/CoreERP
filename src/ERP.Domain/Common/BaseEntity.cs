namespace ERP.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetCreationAudit(DateTime utcNow, string? userName)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = userName;
        UpdatedAtUtc = utcNow;
        UpdatedBy = userName;
    }

    public void SetUpdateAudit(DateTime utcNow, string? userName)
    {
        UpdatedAtUtc = utcNow;
        UpdatedBy = userName;
    }

    public void SoftDelete(DateTime utcNow, string? userName)
    {
        IsDeleted = true;
        SetUpdateAudit(utcNow, userName);
    }
}
