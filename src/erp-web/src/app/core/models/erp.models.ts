import { LookupOption } from './common.models';

export interface BranchDto {
  id: string;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
}

export interface CategoryDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface ProductListItemDto {
  id: string;
  code: string;
  name: string;
  sku: string;
  categoryName: string;
  salePrice: number;
  standardCost: number;
  reorderLevel: number;
  isActive: boolean;
}

export interface ProductDto extends ProductListItemDto {
  description?: string | null;
  categoryId: string;
  unitOfMeasureId?: string | null;
  unitOfMeasureName?: string | null;
  isStockTracked: boolean;
}

export interface CustomerDto {
  id: string;
  code: string;
  name: string;
  taxNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  creditLimit: number;
  paymentTermsDays: number;
  isActive: boolean;
}

export interface SupplierDto {
  id: string;
  code: string;
  name: string;
  taxNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  paymentTermsDays: number;
  isActive: boolean;
}

export interface DashboardDto {
  kpis: Array<{ key: string; label: string; value: number }>;
  trends: Array<{ date: string; sales: number; purchases: number }>;
  topProducts: Array<{ label: string; value: number }>;
  topCustomers: Array<{ label: string; value: number }>;
  lowStockAlerts: Array<{ id: string; title: string; message: string; triggeredAtUtc: string; isRead: boolean }>;
  pendingApprovals: number;
  lowStockCount: number;
  paidInvoices: number;
  openInvoices: number;
}

export interface ApprovalRequestDto {
  id: string;
  documentType: number;
  documentId: string;
  branchId?: string | null;
  branchName?: string | null;
  ruleName: string;
  status: number;
  requestedAtUtc: string;
  requestedBy?: string | null;
  comments?: string | null;
}

export interface ApprovalRuleDto {
  id: string;
  name: string;
  documentType: number;
  branchId?: string | null;
  branchName?: string | null;
  minimumAmount: number;
  maximumAmount?: number | null;
  approverRoleName?: string | null;
  approverUserId?: string | null;
  isActive: boolean;
}

export interface PurchaseOrderListItemDto {
  id: string;
  number: string;
  branchId: string;
  branchName: string;
  supplierId: string;
  supplierName: string;
  orderDateUtc: string;
  status: number;
  totalAmount: number;
}

export interface PurchaseOrderDto extends PurchaseOrderListItemDto {
  expectedDateUtc?: string | null;
  notes?: string | null;
  lines: Array<{
    id: string;
    productId: string;
    productCode: string;
    productName: string;
    orderedQuantity: number;
    receivedQuantity: number;
    unitPrice: number;
    discountPercent: number;
    taxPercent: number;
    lineTotal: number;
    description?: string | null;
  }>;
}

export interface SalesOrderListItemDto {
  id: string;
  number: string;
  branchId: string;
  branchName: string;
  customerId: string;
  customerName: string;
  orderDateUtc: string;
  status: number;
  totalAmount: number;
}

export interface SalesOrderDto extends SalesOrderListItemDto {
  dueDateUtc?: string | null;
  notes?: string | null;
  lines: Array<{
    id: string;
    productId: string;
    productCode: string;
    productName: string;
    orderedQuantity: number;
    deliveredQuantity: number;
    unitPrice: number;
    discountPercent: number;
    taxPercent: number;
    lineTotal: number;
    description?: string | null;
  }>;
}

export interface InvoiceListItemDto {
  id: string;
  number: string;
  branchId: string;
  branchName: string;
  counterpartyId: string;
  counterpartyName: string;
  invoiceDateUtc: string;
  dueDateUtc: string;
  status: number;
  totalAmount: number;
  paidAmount: number;
  returnAmount: number;
  outstandingAmount: number;
}

export interface PaymentDto {
  id: string;
  number: string;
  branchId: string;
  branchName: string;
  type: number;
  paymentDateUtc: string;
  amount: number;
  method: string;
  referenceNumber?: string | null;
  counterpartyName?: string | null;
  invoiceNumber?: string | null;
  status: number;
}

export interface ReturnListItemDto {
  id: string;
  number: string;
  branchId: string;
  branchName: string;
  type: number;
  returnDateUtc: string;
  status: number;
  totalAmount: number;
}

export interface StockBalanceDto {
  branchId: string;
  branchName: string;
  productId: string;
  productCode: string;
  productName: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
  averageCost: number;
  stockValue: number;
  reorderLevel: number;
  isLowStock: boolean;
}

export interface InventoryMovementDto {
  id: string;
  branchId: string;
  branchName: string;
  productId: string;
  productCode: string;
  productName: string;
  movementDateUtc: string;
  type: number;
  quantity: number;
  unitCost: number;
  quantityAfter: number;
  averageCostAfter: number;
  referenceNumber: string;
  referenceDocumentType?: string | null;
  referenceDocumentId?: string | null;
  remarks?: string | null;
}

export interface LowStockItemDto {
  branchId: string;
  branchName: string;
  productId: string;
  productCode: string;
  productName: string;
  quantityOnHand: number;
  reorderLevel: number;
}

export interface AlertDto {
  id: string;
  type: number;
  branchId: string;
  branchName: string;
  title: string;
  message: string;
  isRead: boolean;
  isActive: boolean;
  triggeredAtUtc: string;
}

export interface AuditLogDto {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  userName?: string | null;
  branchId?: string | null;
  ipAddress?: string | null;
  timestampUtc: string;
  beforeData?: string | null;
  afterData?: string | null;
}

export interface UserListItemDto {
  id: string;
  userName: string;
  email?: string | null;
  isActive: boolean;
  roles: string[];
  branchIds: string[];
}

export interface UserDetailDto extends UserListItemDto {
  defaultBranchId?: string | null;
}

export interface RoleDto {
  id: string;
  name: string;
  description?: string | null;
  permissionCodes: string[];
}

export interface PermissionDto {
  id: string;
  module: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface ReferenceDataBundle {
  branches: LookupOption[];
  categories: LookupOption[];
  products: LookupOption[];
  customers: LookupOption[];
  suppliers: LookupOption[];
}
