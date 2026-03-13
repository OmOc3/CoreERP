using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Application.Common.Contracts;

public interface IErpDbContext
{
    DbSet<Branch> Branches { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserBranchAccess> UserBranchAccesses { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderLine> SalesOrderLines { get; }
    DbSet<PurchaseInvoice> PurchaseInvoices { get; }
    DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines { get; }
    DbSet<SalesInvoice> SalesInvoices { get; }
    DbSet<SalesInvoiceLine> SalesInvoiceLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<ReturnDocument> ReturnDocuments { get; }
    DbSet<ReturnLine> ReturnLines { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<ApprovalRule> ApprovalRules { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Alert> Alerts { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
