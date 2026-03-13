using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class StockBalance : BranchScopedEntity, IAggregateRoot
{
    private StockBalance()
    {
    }

    public StockBalance(Guid branchId, Guid productId)
    {
        BranchId = branchId;
        ProductId = productId;
    }

    public Branch? Branch { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AverageCost { get; private set; }
    public decimal AvailableQuantity => QuantityOnHand - ReservedQuantity;
    public decimal StockValue => QuantityOnHand * AverageCost;

    public void Receive(decimal quantity, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Receipt quantity must be greater than zero.");
        }

        var existingValue = QuantityOnHand * AverageCost;
        var incomingValue = quantity * unitCost;
        QuantityOnHand += quantity;
        AverageCost = QuantityOnHand == 0
            ? 0
            : decimal.Round((existingValue + incomingValue) / QuantityOnHand, 4, MidpointRounding.AwayFromZero);
    }

    public void Issue(decimal quantity, bool allowNegativeStock = false)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Issue quantity must be greater than zero.");
        }

        if (!allowNegativeStock && quantity > AvailableQuantity)
        {
            throw new DomainRuleException("Insufficient available stock.");
        }

        QuantityOnHand -= quantity;
        if (QuantityOnHand == 0)
        {
            AverageCost = 0;
        }
    }

    public void Adjust(decimal quantityDifference, decimal unitCost)
    {
        if (quantityDifference > 0)
        {
            Receive(quantityDifference, unitCost);
            return;
        }

        if (quantityDifference < 0)
        {
            Issue(Math.Abs(quantityDifference));
        }
    }
}
