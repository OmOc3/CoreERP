using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class ReturnDocument : BranchScopedEntity, IAggregateRoot
{
    private readonly List<ReturnLine> _lines = [];

    private ReturnDocument()
    {
    }

    public ReturnDocument(
        string number,
        ReturnDocumentType type,
        Guid branchId,
        DateTime returnDateUtc,
        Guid? customerId,
        Guid? supplierId,
        Guid? salesInvoiceId,
        Guid? purchaseInvoiceId,
        string? reason)
    {
        Number = number.Trim().ToUpperInvariant();
        Type = type;
        BranchId = branchId;
        ReturnDateUtc = returnDateUtc;
        CustomerId = customerId;
        SupplierId = supplierId;
        SalesInvoiceId = salesInvoiceId;
        PurchaseInvoiceId = purchaseInvoiceId;
        Reason = reason?.Trim();
        Status = ReturnStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;
    public ReturnDocumentType Type { get; private set; }
    public Branch? Branch { get; private set; }
    public DateTime ReturnDateUtc { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Supplier? Supplier { get; private set; }
    public Guid? SalesInvoiceId { get; private set; }
    public SalesInvoice? SalesInvoice { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }
    public PurchaseInvoice? PurchaseInvoice { get; private set; }
    public string? Reason { get; private set; }
    public ReturnStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<ReturnLine> Lines => _lines;

    public void ReplaceLines(IEnumerable<ReturnLine> lines)
    {
        if (Status != ReturnStatus.Draft)
        {
            throw new DomainRuleException("Only draft returns can be edited.");
        }

        var materializedLines = lines.ToList();
        if (materializedLines.Count == 0)
        {
            throw new DomainRuleException("Return document must contain at least one line.");
        }

        _lines.Clear();
        _lines.AddRange(materializedLines);
        TotalAmount = _lines.Sum(x => x.LineTotal);
    }

    public void Post()
    {
        if (Status != ReturnStatus.Draft)
        {
            throw new DomainRuleException("Only draft returns can be posted.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainRuleException("Return document must contain at least one line.");
        }

        Status = ReturnStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == ReturnStatus.Cancelled)
        {
            throw new DomainRuleException("Return is already cancelled.");
        }

        Status = ReturnStatus.Cancelled;
    }
}
