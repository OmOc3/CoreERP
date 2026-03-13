using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class UnitOfMeasure : BaseEntity, IAggregateRoot
{
    private UnitOfMeasure()
    {
    }

    public UnitOfMeasure(string name, string symbol)
    {
        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
    }

    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;

    public void Update(string name, string symbol)
    {
        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
    }
}
