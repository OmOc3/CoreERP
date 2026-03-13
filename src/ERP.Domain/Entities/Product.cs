using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Product : BaseEntity, IAggregateRoot
{
    private Product()
    {
    }

    public Product(
        string code,
        string name,
        string sku,
        Guid categoryId,
        Guid? unitOfMeasureId,
        decimal reorderLevel,
        decimal standardCost,
        decimal salePrice,
        bool isStockTracked,
        string? description)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        SKU = sku.Trim().ToUpperInvariant();
        CategoryId = categoryId;
        UnitOfMeasureId = unitOfMeasureId;
        ReorderLevel = reorderLevel;
        StandardCost = standardCost;
        SalePrice = salePrice;
        IsStockTracked = isStockTracked;
        Description = description?.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public ProductCategory? Category { get; private set; }
    public Guid? UnitOfMeasureId { get; private set; }
    public UnitOfMeasure? UnitOfMeasure { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public decimal StandardCost { get; private set; }
    public decimal SalePrice { get; private set; }
    public bool IsStockTracked { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        string sku,
        Guid categoryId,
        Guid? unitOfMeasureId,
        decimal reorderLevel,
        decimal standardCost,
        decimal salePrice,
        bool isStockTracked,
        bool isActive,
        string? description)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        SKU = sku.Trim().ToUpperInvariant();
        CategoryId = categoryId;
        UnitOfMeasureId = unitOfMeasureId;
        ReorderLevel = reorderLevel;
        StandardCost = standardCost;
        SalePrice = salePrice;
        IsStockTracked = isStockTracked;
        IsActive = isActive;
        Description = description?.Trim();
    }
}
