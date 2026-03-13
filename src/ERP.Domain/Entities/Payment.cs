using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class Payment : BranchScopedEntity, IAggregateRoot
{
    private Payment()
    {
    }

    public Payment(
        string number,
        Guid branchId,
        PaymentType type,
        DateTime paymentDateUtc,
        decimal amount,
        string method,
        string? referenceNumber,
        Guid? customerId,
        Guid? supplierId,
        Guid? salesInvoiceId,
        Guid? purchaseInvoiceId,
        string? notes)
    {
        Number = number.Trim().ToUpperInvariant();
        BranchId = branchId;
        Type = type;
        PaymentDateUtc = paymentDateUtc;
        Amount = amount;
        Method = method.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        CustomerId = customerId;
        SupplierId = supplierId;
        SalesInvoiceId = salesInvoiceId;
        PurchaseInvoiceId = purchaseInvoiceId;
        Notes = notes?.Trim();
        Status = PaymentStatus.Posted;
    }

    public string Number { get; private set; } = string.Empty;
    public Branch? Branch { get; private set; }
    public PaymentType Type { get; private set; }
    public DateTime PaymentDateUtc { get; private set; }
    public decimal Amount { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string? ReferenceNumber { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Supplier? Supplier { get; private set; }
    public Guid? SalesInvoiceId { get; private set; }
    public SalesInvoice? SalesInvoice { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }
    public PurchaseInvoice? PurchaseInvoice { get; private set; }
    public string? Notes { get; private set; }
    public PaymentStatus Status { get; private set; }

    public void Void()
    {
        if (Status == PaymentStatus.Voided)
        {
            throw new DomainRuleException("Payment is already voided.");
        }

        Status = PaymentStatus.Voided;
    }
}
