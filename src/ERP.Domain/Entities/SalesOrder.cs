using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SalesOrder : BranchScopedEntity, IAggregateRoot
{
    private readonly List<SalesOrderLine> _lines = [];

    private SalesOrder()
    {
    }

    public SalesOrder(string number, Guid customerId, Guid branchId, DateTime orderDateUtc, DateTime? dueDateUtc, string? notes)
    {
        Number = number.Trim().ToUpperInvariant();
        CustomerId = customerId;
        BranchId = branchId;
        OrderDateUtc = orderDateUtc;
        DueDateUtc = dueDateUtc;
        Notes = notes?.Trim();
        Status = SalesOrderStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Branch? Branch { get; private set; }
    public DateTime OrderDateUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public string? Notes { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines;

    public void UpdateHeader(Guid customerId, Guid branchId, DateTime orderDateUtc, DateTime? dueDateUtc, string? notes)
    {
        if (Status is not SalesOrderStatus.Draft and not SalesOrderStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected sales orders can be edited.");
        }

        CustomerId = customerId;
        BranchId = branchId;
        OrderDateUtc = orderDateUtc;
        DueDateUtc = dueDateUtc;
        Notes = notes?.Trim();
    }

    public void ReplaceLines(IEnumerable<SalesOrderLine> lines)
    {
        if (Status is not SalesOrderStatus.Draft and not SalesOrderStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected sales orders can be edited.");
        }

        var materializedLines = lines.ToList();
        if (materializedLines.Count == 0)
        {
            throw new DomainRuleException("Sales order must contain at least one line.");
        }

        _lines.Clear();
        _lines.AddRange(materializedLines);
        TotalAmount = _lines.Sum(x => x.LineTotal);
    }

    public void SubmitForApproval()
    {
        if (_lines.Count == 0)
        {
            throw new DomainRuleException("Sales order must contain at least one line.");
        }

        if (Status is not SalesOrderStatus.Draft and not SalesOrderStatus.Rejected)
        {
            throw new DomainRuleException("Only draft or rejected sales orders can be submitted.");
        }

        Status = SalesOrderStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != SalesOrderStatus.PendingApproval)
        {
            throw new DomainRuleException("Sales order must be pending approval.");
        }

        Status = SalesOrderStatus.Approved;
    }

    public void Reject()
    {
        if (Status != SalesOrderStatus.PendingApproval)
        {
            throw new DomainRuleException("Sales order must be pending approval.");
        }

        Status = SalesOrderStatus.Rejected;
    }

    public void RegisterDelivery(Guid lineId, decimal quantity)
    {
        if (Status is not SalesOrderStatus.Approved and not SalesOrderStatus.PartiallyDelivered)
        {
            throw new DomainRuleException("Only approved sales orders can be delivered.");
        }

        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new DomainRuleException("Sales order line was not found.");

        line.RegisterDelivery(quantity);
        Status = _lines.All(x => x.IsFullyDelivered)
            ? SalesOrderStatus.Completed
            : SalesOrderStatus.PartiallyDelivered;
    }

    public void Cancel()
    {
        if (Status is SalesOrderStatus.Completed or SalesOrderStatus.Cancelled)
        {
            throw new DomainRuleException("Completed or cancelled sales orders cannot be changed.");
        }

        Status = SalesOrderStatus.Cancelled;
    }
}
