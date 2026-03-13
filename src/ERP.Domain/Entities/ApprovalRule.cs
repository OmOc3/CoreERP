using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class ApprovalRule : BaseEntity, IAggregateRoot
{
    private ApprovalRule()
    {
    }

    public ApprovalRule(
        string name,
        ApprovalDocumentType documentType,
        Guid? branchId,
        decimal minimumAmount,
        decimal? maximumAmount,
        string? approverRoleName,
        Guid? approverUserId)
    {
        Name = name.Trim();
        DocumentType = documentType;
        BranchId = branchId;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        ApproverRoleName = approverRoleName?.Trim();
        ApproverUserId = approverUserId;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public ApprovalDocumentType DocumentType { get; private set; }
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public decimal MinimumAmount { get; private set; }
    public decimal? MaximumAmount { get; private set; }
    public string? ApproverRoleName { get; private set; }
    public Guid? ApproverUserId { get; private set; }
    public bool IsActive { get; private set; }

    public bool Matches(Guid? branchId, ApprovalDocumentType documentType, decimal amount)
    {
        var branchMatches = BranchId == null || BranchId == branchId;
        var documentMatches = DocumentType == documentType;
        var minMatches = amount >= MinimumAmount;
        var maxMatches = !MaximumAmount.HasValue || amount <= MaximumAmount.Value;

        return IsActive && branchMatches && documentMatches && minMatches && maxMatches;
    }

    public void Update(string name, Guid? branchId, decimal minimumAmount, decimal? maximumAmount, string? approverRoleName, Guid? approverUserId, bool isActive)
    {
        Name = name.Trim();
        BranchId = branchId;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        ApproverRoleName = approverRoleName?.Trim();
        ApproverUserId = approverUserId;
        IsActive = isActive;
    }
}
