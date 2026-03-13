using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class ProductCategory : BaseEntity, IAggregateRoot
{
    private ProductCategory()
    {
    }

    public ProductCategory(string code, string name, string? description)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public void Update(string code, string name, string? description)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
    }
}
