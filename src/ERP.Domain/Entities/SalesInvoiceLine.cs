using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class SalesInvoiceLine : BaseEntity
{
    private SalesInvoiceLine()
    {
    }

    public SalesInvoiceLine(Guid productId, decimal quantity, decimal unitPrice, decimal taxPercent, Guid? salesOrderLineId)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Invoice quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxPercent = taxPercent;
        SalesOrderLineId = salesOrderLineId;
        Recalculate();
    }

    public Guid SalesInvoiceId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }

    private void Recalculate()
    {
        var gross = Quantity * UnitPrice;
        var tax = gross * (TaxPercent / 100m);
        LineTotal = decimal.Round(gross + tax, 2, MidpointRounding.AwayFromZero);
    }
}
