using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SalesInvoice : BranchScopedEntity, IAggregateRoot
{
    private readonly List<SalesInvoiceLine> _lines = [];

    private SalesInvoice()
    {
    }

    public SalesInvoice(string number, Guid customerId, Guid branchId, Guid? salesOrderId, DateTime invoiceDateUtc, DateTime dueDateUtc, string? notes)
    {
        Number = number.Trim().ToUpperInvariant();
        CustomerId = customerId;
        BranchId = branchId;
        SalesOrderId = salesOrderId;
        InvoiceDateUtc = invoiceDateUtc;
        DueDateUtc = dueDateUtc;
        Notes = notes?.Trim();
        Status = InvoiceStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public SalesOrder? SalesOrder { get; private set; }
    public DateTime InvoiceDateUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal ReturnAmount { get; private set; }
    public decimal OutstandingAmount => TotalAmount - PaidAmount - ReturnAmount;
    public IReadOnlyCollection<SalesInvoiceLine> Lines => _lines;

    public void ReplaceLines(IEnumerable<SalesInvoiceLine> lines)
    {
        if (Status is not InvoiceStatus.Draft and not InvoiceStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected sales invoices can be edited.");
        }

        var materializedLines = lines.ToList();
        if (materializedLines.Count == 0)
        {
            throw new DomainRuleException("Sales invoice must contain at least one line.");
        }

        _lines.Clear();
        _lines.AddRange(materializedLines);
        TotalAmount = _lines.Sum(x => x.LineTotal);
    }

    public void SubmitForApproval()
    {
        if (_lines.Count == 0)
        {
            throw new DomainRuleException("Sales invoice must contain at least one line.");
        }

        Status = InvoiceStatus.PendingApproval;
    }

    public void Post()
    {
        if (Status is not InvoiceStatus.Draft and not InvoiceStatus.PendingApproval)
        {
            throw new DomainRuleException("Only draft or approved sales invoices can be posted.");
        }

        Status = InvoiceStatus.Posted;
    }

    public void Reject()
    {
        if (Status != InvoiceStatus.PendingApproval)
        {
            throw new DomainRuleException("Sales invoice must be pending approval.");
        }

        Status = InvoiceStatus.Rejected;
    }

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("Payment amount must be greater than zero.");
        }

        if (amount > OutstandingAmount)
        {
            throw new DomainRuleException("Payment amount exceeds outstanding receivable.");
        }

        PaidAmount += amount;
        Status = OutstandingAmount == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }

    public void ApplyReturn(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("Return amount must be greater than zero.");
        }

        ReturnAmount += amount;
        Status = OutstandingAmount == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }
}
