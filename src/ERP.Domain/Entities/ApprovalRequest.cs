using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class ApprovalRequest : BaseEntity, IAggregateRoot
{
    private ApprovalRequest()
    {
    }

    public ApprovalRequest(
        Guid ruleId,
        ApprovalDocumentType documentType,
        Guid documentId,
        Guid? branchId,
        Guid requestedByUserId)
    {
        RuleId = ruleId;
        DocumentType = documentType;
        DocumentId = documentId;
        BranchId = branchId;
        RequestedByUserId = requestedByUserId;
        Status = ApprovalStatus.Pending;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public Guid RuleId { get; private set; }
    public ApprovalRule? Rule { get; private set; }
    public ApprovalDocumentType DocumentType { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? Comments { get; private set; }

    public void Approve(Guid reviewedByUserId, string? comments)
    {
        if (Status != ApprovalStatus.Pending)
        {
            throw new DomainRuleException("Approval request is not pending.");
        }

        Status = ApprovalStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        Comments = comments?.Trim();
    }

    public void Reject(Guid reviewedByUserId, string? comments)
    {
        if (Status != ApprovalStatus.Pending)
        {
            throw new DomainRuleException("Approval request is not pending.");
        }

        Status = ApprovalStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        Comments = comments?.Trim();
    }
}
