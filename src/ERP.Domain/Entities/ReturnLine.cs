using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class ReturnLine : BaseEntity
{
    private ReturnLine()
    {
    }

    public ReturnLine(Guid productId, decimal quantity, decimal unitPrice, string? reason)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Return quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Reason = reason?.Trim();
        LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public Guid ReturnDocumentId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Reason { get; private set; }
}
