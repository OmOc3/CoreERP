using ERP.Application.Common.Contracts;
using ERP.Domain.Entities;
using ERP.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>,
        IErpDbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReturnDocument> ReturnDocuments => Set<ReturnDocument>();
    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    DatabaseFacade IErpDbContext.Database => base.Database;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        ConfigureAuditableEntity<Branch>(builder.Entity<Branch>());
        ConfigureAuditableEntity<UnitOfMeasure>(builder.Entity<UnitOfMeasure>());
        ConfigureAuditableEntity<ProductCategory>(builder.Entity<ProductCategory>());
        ConfigureAuditableEntity<Product>(builder.Entity<Product>());
        ConfigureAuditableEntity<Customer>(builder.Entity<Customer>());
        ConfigureAuditableEntity<Supplier>(builder.Entity<Supplier>());
        ConfigureAuditableEntity<Permission>(builder.Entity<Permission>());
        ConfigureAuditableEntity<UserBranchAccess>(builder.Entity<UserBranchAccess>());
        ConfigureAuditableEntity<RefreshToken>(builder.Entity<RefreshToken>());
        ConfigureAuditableEntity<PurchaseOrder>(builder.Entity<PurchaseOrder>());
        ConfigureAuditableEntity<PurchaseOrderLine>(builder.Entity<PurchaseOrderLine>());
        ConfigureAuditableEntity<SalesOrder>(builder.Entity<SalesOrder>());
        ConfigureAuditableEntity<SalesOrderLine>(builder.Entity<SalesOrderLine>());
        ConfigureAuditableEntity<PurchaseInvoice>(builder.Entity<PurchaseInvoice>());
        ConfigureAuditableEntity<PurchaseInvoiceLine>(builder.Entity<PurchaseInvoiceLine>());
        ConfigureAuditableEntity<SalesInvoice>(builder.Entity<SalesInvoice>());
        ConfigureAuditableEntity<SalesInvoiceLine>(builder.Entity<SalesInvoiceLine>());
        ConfigureAuditableEntity<Payment>(builder.Entity<Payment>());
        ConfigureAuditableEntity<ReturnDocument>(builder.Entity<ReturnDocument>());
        ConfigureAuditableEntity<ReturnLine>(builder.Entity<ReturnLine>());
        ConfigureAuditableEntity<InventoryMovement>(builder.Entity<InventoryMovement>());
        ConfigureAuditableEntity<StockBalance>(builder.Entity<StockBalance>());
        ConfigureAuditableEntity<ApprovalRule>(builder.Entity<ApprovalRule>());
        ConfigureAuditableEntity<ApprovalRequest>(builder.Entity<ApprovalRequest>());
        ConfigureAuditableEntity<AuditLog>(builder.Entity<AuditLog>());
        ConfigureAuditableEntity<Alert>(builder.Entity<Alert>());

        builder.Entity<NumberSequence>(entity =>
        {
            entity.ToTable("NumberSequences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Prefix).IsUnique();
            entity.Property(x => x.Prefix).HasMaxLength(32);
        });

        builder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
        });

        builder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("UnitsOfMeasure");
            entity.HasIndex(x => x.Symbol).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.Property(x => x.Symbol).HasMaxLength(16);
        });

        builder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("ProductCategories");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.SKU).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.SKU).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.Property(x => x.ReorderLevel).HasColumnType("decimal(18,4)");
            entity.Property(x => x.StandardCost).HasColumnType("decimal(18,4)");
            entity.Property(x => x.SalePrice).HasColumnType("decimal(18,4)");
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UnitOfMeasure).WithMany().HasForeignKey(x => x.UnitOfMeasureId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Email).HasMaxLength(128);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Address).HasMaxLength(256);
            entity.Property(x => x.CreditLimit).HasColumnType("decimal(18,4)");
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Email).HasMaxLength(128);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Address).HasMaxLength(256);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Module).HasMaxLength(64);
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserBranchAccess>(entity =>
        {
            entity.ToTable("UserBranchAccesses");
            entity.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(256);
            entity.Property(x => x.CreatedByIp).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(256);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Notes).HasMaxLength(512);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(PurchaseOrder.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("PurchaseOrderLines");
            entity.Property(x => x.OrderedQuantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReceivedQuantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.DiscountPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.TaxPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.HasOne<PurchaseOrder>().WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("SalesOrders");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Notes).HasMaxLength(512);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(SalesOrder.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<SalesOrderLine>(entity =>
        {
            entity.ToTable("SalesOrderLines");
            entity.Property(x => x.OrderedQuantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.DeliveredQuantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.DiscountPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.TaxPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.HasOne<SalesOrder>().WithMany(x => x.Lines).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseInvoice>(entity =>
        {
            entity.ToTable("PurchaseInvoices");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.PaidAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReturnAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Notes).HasMaxLength(512);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(PurchaseInvoice.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<PurchaseInvoiceLine>(entity =>
        {
            entity.ToTable("PurchaseInvoiceLines");
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.TaxPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
            entity.HasOne(x => x.PurchaseInvoice).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesInvoice>(entity =>
        {
            entity.ToTable("SalesInvoices");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.PaidAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReturnAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Notes).HasMaxLength(512);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(SalesInvoice.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<SalesInvoiceLine>(entity =>
        {
            entity.ToTable("SalesInvoiceLines");
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.TaxPercent).HasColumnType("decimal(18,4)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
            entity.HasOne(x => x.SalesInvoice).WithMany(x => x.Lines).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SalesOrderLine>().WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Method).HasMaxLength(64);
            entity.Property(x => x.ReferenceNumber).HasMaxLength(64);
            entity.Property(x => x.Notes).HasMaxLength(256);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReturnDocument>(entity =>
        {
            entity.ToTable("ReturnDocuments");
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(256);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(ReturnDocument.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ReturnLine>(entity =>
        {
            entity.ToTable("ReturnLines");
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Reason).HasMaxLength(256);
            entity.HasOne<ReturnDocument>().WithMany(x => x.Lines).HasForeignKey(x => x.ReturnDocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InventoryMovement>(entity =>
        {
            entity.ToTable("InventoryMovements");
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(x => x.QuantityAfter).HasColumnType("decimal(18,4)");
            entity.Property(x => x.AverageCostAfter).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReferenceNumber).HasMaxLength(32);
            entity.Property(x => x.ReferenceDocumentType).HasMaxLength(64);
            entity.Property(x => x.Remarks).HasMaxLength(256);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("StockBalances");
            entity.HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();
            entity.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReservedQuantity).HasColumnType("decimal(18,4)");
            entity.Property(x => x.AverageCost).HasColumnType("decimal(18,4)");
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApprovalRule>(entity =>
        {
            entity.ToTable("ApprovalRules");
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.MinimumAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.MaximumAmount).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ApproverRoleName).HasMaxLength(64);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("ApprovalRequests");
            entity.Property(x => x.Comments).HasMaxLength(512);
            entity.HasOne(x => x.Rule).WithMany().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.EntityName).HasMaxLength(128);
            entity.Property(x => x.EntityId).HasMaxLength(64);
            entity.Property(x => x.Action).HasMaxLength(64);
            entity.Property(x => x.UserName).HasMaxLength(128);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
        });

        builder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.Property(x => x.Title).HasMaxLength(128);
            entity.Property(x => x.Message).HasMaxLength(512);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditableEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : ERP.Domain.Common.BaseEntity
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.CreatedAtUtc);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedAtUtc);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        var rowVersion = entity.Property(x => x.RowVersion);
        if (Database.IsSqlite())
        {
            rowVersion
                .IsConcurrencyToken()
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("randomblob(8)");
        }
        else
        {
            rowVersion.IsRowVersion();
        }
    }
}
