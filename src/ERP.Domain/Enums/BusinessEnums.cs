namespace ERP.Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    PartiallyReceived = 4,
    Completed = 5,
    Cancelled = 6,
    Rejected = 7
}

public enum SalesOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    PartiallyDelivered = 4,
    Completed = 5,
    Cancelled = 6,
    Rejected = 7
}

public enum InvoiceStatus
{
    Draft = 1,
    PendingApproval = 2,
    Posted = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Cancelled = 6,
    Rejected = 7
}

public enum PaymentStatus
{
    Posted = 1,
    Voided = 2
}

public enum PaymentType
{
    CustomerReceipt = 1,
    SupplierPayment = 2
}

public enum ReturnDocumentType
{
    SalesReturn = 1,
    PurchaseReturn = 2
}

public enum ReturnStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}

public enum InventoryMovementType
{
    OpeningBalance = 1,
    PurchaseReceipt = 2,
    SaleIssue = 3,
    SalesReturn = 4,
    PurchaseReturn = 5,
    StockAdjustmentIncrease = 6,
    StockAdjustmentDecrease = 7,
    TransferIn = 8,
    TransferOut = 9
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ApprovalDocumentType
{
    PurchaseOrder = 1,
    SalesOrder = 2,
    PurchaseInvoice = 3,
    SalesInvoice = 4
}

public enum CounterpartyType
{
    Customer = 1,
    Supplier = 2
}

public enum AlertType
{
    LowStock = 1,
    Workflow = 2
}
