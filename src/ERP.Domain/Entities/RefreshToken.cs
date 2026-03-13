using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string? ReplacedByToken { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
