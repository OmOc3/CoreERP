namespace ERP.Infrastructure.Persistence;

public sealed class NumberSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Prefix { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
