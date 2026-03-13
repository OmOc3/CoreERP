using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class SalesOrderLine : BaseEntity
{
    private SalesOrderLine()
    {
    }

    public SalesOrderLine(Guid productId, decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent, string? description)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Ordered quantity must be greater than zero.");
        }

        ProductId = productId;
        OrderedQuantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
        Description = description?.Trim();
        Recalculate();
    }

    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal DeliveredQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Description { get; private set; }
    public bool IsFullyDelivered => DeliveredQuantity >= OrderedQuantity;

    public void RegisterDelivery(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Delivery quantity must be greater than zero.");
        }

        if (DeliveredQuantity + quantity > OrderedQuantity)
        {
            throw new DomainRuleException("Delivery quantity exceeds ordered quantity.");
        }

        DeliveredQuantity += quantity;
    }

    private void Recalculate()
    {
        var gross = OrderedQuantity * UnitPrice;
        var discount = gross * (DiscountPercent / 100m);
        var taxable = gross - discount;
        var tax = taxable * (TaxPercent / 100m);
        LineTotal = decimal.Round(taxable + tax, 2, MidpointRounding.AwayFromZero);
    }
}
