using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class PurchaseOrder : BranchScopedEntity, IAggregateRoot
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    public PurchaseOrder(string number, Guid supplierId, Guid branchId, DateTime orderDateUtc, DateTime? expectedDateUtc, string? notes)
    {
        Number = number.Trim().ToUpperInvariant();
        SupplierId = supplierId;
        BranchId = branchId;
        OrderDateUtc = orderDateUtc;
        ExpectedDateUtc = expectedDateUtc;
        Notes = notes?.Trim();
        Status = PurchaseOrderStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Supplier? Supplier { get; private set; }
    public Branch? Branch { get; private set; }
    public DateTime OrderDateUtc { get; private set; }
    public DateTime? ExpectedDateUtc { get; private set; }
    public string? Notes { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines;

    public void ReplaceLines(IEnumerable<PurchaseOrderLine> lines)
    {
        if (Status is not PurchaseOrderStatus.Draft and not PurchaseOrderStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected purchase orders can be edited.");
        }

        var materializedLines = lines.ToList();
        if (materializedLines.Count == 0)
        {
            throw new DomainRuleException("Purchase order must contain at least one line.");
        }

        _lines.Clear();
        _lines.AddRange(materializedLines);
        RecalculateTotals();
    }

    public void SubmitForApproval()
    {
        EnsureHasLines();
        if (Status is not PurchaseOrderStatus.Draft and not PurchaseOrderStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected purchase orders can be submitted.");
        }

        Status = PurchaseOrderStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
        {
            throw new DomainRuleException("Purchase order must be pending approval.");
        }

        Status = PurchaseOrderStatus.Approved;
    }

    public void Reject()
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
        {
            throw new DomainRuleException("Purchase order must be pending approval.");
        }

        Status = PurchaseOrderStatus.Rejected;
    }

    public void RegisterReceipt(Guid lineId, decimal quantity)
    {
        if (Status is not PurchaseOrderStatus.Approved and not PurchaseOrderStatus.PartiallyReceived)
        {
            throw new DomainRuleException("Only approved purchase orders can receive stock.");
        }

        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new DomainRuleException("Purchase order line was not found.");

        line.RegisterReceipt(quantity);
        Status = _lines.All(x => x.IsFullyReceived)
            ? PurchaseOrderStatus.Completed
            : PurchaseOrderStatus.PartiallyReceived;
        RecalculateTotals();
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.Completed or PurchaseOrderStatus.Cancelled)
        {
            throw new DomainRuleException("Completed or cancelled purchase orders cannot be changed.");
        }

        Status = PurchaseOrderStatus.Cancelled;
    }

    private void EnsureHasLines()
    {
        if (_lines.Count == 0)
        {
            throw new DomainRuleException("Purchase order must contain at least one line.");
        }
    }

    private void RecalculateTotals()
    {
        TotalAmount = _lines.Sum(x => x.LineTotal);
    }
}
