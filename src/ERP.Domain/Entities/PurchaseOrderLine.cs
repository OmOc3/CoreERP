using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PurchaseOrderLine : BaseEntity
{
    private PurchaseOrderLine()
    {
    }

    public PurchaseOrderLine(Guid productId, decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent, string? description)
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

    public Guid PurchaseOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Description { get; private set; }
    public bool IsFullyReceived => ReceivedQuantity >= OrderedQuantity;

    public void RegisterReceipt(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Receipt quantity must be greater than zero.");
        }

        if (ReceivedQuantity + quantity > OrderedQuantity)
        {
            throw new DomainRuleException("Receipt quantity exceeds ordered quantity.");
        }

        ReceivedQuantity += quantity;
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
