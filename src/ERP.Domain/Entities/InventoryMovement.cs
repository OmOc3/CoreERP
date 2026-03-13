using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class InventoryMovement : BranchScopedEntity
{
    private InventoryMovement()
    {
    }

    public InventoryMovement(
        Guid branchId,
        Guid productId,
        DateTime movementDateUtc,
        InventoryMovementType type,
        decimal quantity,
        decimal unitCost,
        decimal quantityAfter,
        decimal averageCostAfter,
        string referenceNumber,
        string? referenceDocumentType,
        Guid? referenceDocumentId,
        string? remarks)
    {
        BranchId = branchId;
        ProductId = productId;
        MovementDateUtc = movementDateUtc;
        Type = type;
        Quantity = quantity;
        UnitCost = unitCost;
        QuantityAfter = quantityAfter;
        AverageCostAfter = averageCostAfter;
        ReferenceNumber = referenceNumber;
        ReferenceDocumentType = referenceDocumentType;
        ReferenceDocumentId = referenceDocumentId;
        Remarks = remarks;
    }

    public Branch? Branch { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public DateTime MovementDateUtc { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string? ReferenceDocumentType { get; private set; }
    public Guid? ReferenceDocumentId { get; private set; }
    public string? Remarks { get; private set; }
}
