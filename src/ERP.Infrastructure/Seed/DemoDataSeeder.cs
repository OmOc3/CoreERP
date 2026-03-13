using ERP.Application.Common.Security;
using ERP.Domain.Common;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Auth;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Seed;

public sealed class DemoDataSeeder
{
    private readonly ErpDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILowStockAlertService _lowStockAlertService;

    public DemoDataSeeder(
        ErpDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILowStockAlertService lowStockAlertService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _lowStockAlertService = lowStockAlertService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedMasterDataAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedTransactionsAsync(cancellationToken);
        await _lowStockAlertService.GenerateAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _dbContext.Permissions.Select(x => x.Code).ToListAsync(cancellationToken);
        var newPermissions = PermissionCatalog.GetAll().Where(x => !existingCodes.Contains(x.Code)).ToList();
        if (newPermissions.Count == 0)
        {
            return;
        }

        foreach (var permission in newPermissions)
        {
            permission.SetCreationAudit(DateTime.UtcNow, "seed");
        }

        _dbContext.Permissions.AddRange(newPermissions);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(SystemRoles.Admin, "System administrators", PermissionCatalog.GetAll().Select(x => x.Code).ToList(), cancellationToken);
        await EnsureRoleAsync(SystemRoles.Manager, "Branch and commercial managers", PermissionCatalog.GetAll()
            .Where(x => x.Code is not PermissionCatalog.Roles.Manage and not PermissionCatalog.Users.Manage)
            .Select(x => x.Code)
            .ToList(), cancellationToken);
        await EnsureRoleAsync(SystemRoles.BranchUser, "Operational branch user", new[]
        {
            PermissionCatalog.Dashboard.View,
            PermissionCatalog.Products.View,
            PermissionCatalog.Categories.View,
            PermissionCatalog.Customers.View,
            PermissionCatalog.Suppliers.View,
            PermissionCatalog.PurchaseOrders.View,
            PermissionCatalog.PurchaseOrders.Manage,
            PermissionCatalog.PurchaseOrders.Receive,
            PermissionCatalog.SalesOrders.View,
            PermissionCatalog.SalesOrders.Manage,
            PermissionCatalog.SalesOrders.Invoice,
            PermissionCatalog.Invoices.View,
            PermissionCatalog.Payments.View,
            PermissionCatalog.Payments.Manage,
            PermissionCatalog.Returns.View,
            PermissionCatalog.Returns.Manage,
            PermissionCatalog.Inventory.View,
            PermissionCatalog.Alerts.View,
            PermissionCatalog.Reports.View
        }, cancellationToken);
    }

    private async Task SeedMasterDataAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        var hq = new Branch("HQ", "Head Office", "Cairo Business Park", "01000000001", "hq@erp.local");
        var alex = new Branch("ALX", "Alexandria Branch", "Alex Corniche", "01000000002", "alex@erp.local");
        var each = new UnitOfMeasure("Each", "EA");
        var box = new UnitOfMeasure("Box", "BOX");
        var electronics = new ProductCategory("ELEC", "Electronics", "Sellable electronics");
        var office = new ProductCategory("OFF", "Office Supplies", "Office consumables");

        var laptop = new Product("PRD-LAP", "Business Laptop 14", "LAP-14", electronics.Id, each.Id, 4, 600m, 950m, true, "Core business laptop");
        var paper = new Product("PRD-PAPER", "A4 Printer Paper", "PAPER-A4", office.Id, box.Id, 40, 4m, 7m, true, "A4 copy paper");
        var toner = new Product("PRD-TONER", "Laser Toner Cartridge", "TONER-01", office.Id, each.Id, 6, 45m, 75m, true, "Shared printer toner");

        var customer1 = new Customer("CUS-ACME", "Acme Retail", "TX-100", "ap@acme.local", "01110000001", "Nasr City", 15000m, 30);
        var customer2 = new Customer("CUS-NILE", "Nile Stores", "TX-101", "finance@nile.local", "01110000002", "Heliopolis", 12000m, 21);
        var supplier1 = new Supplier("SUP-TECH", "Tech Source", "TAX-200", "sales@techsource.local", "01220000001", "Smart Village", 30);
        var supplier2 = new Supplier("SUP-OFFICE", "Office Hub", "TAX-201", "contact@officehub.local", "01220000002", "Giza", 14);

        var masterData = new BaseEntity[]
        {
            hq, alex, each, box, electronics, office, laptop, paper, toner, customer1, customer2, supplier1, supplier2
        };

        foreach (var entity in masterData)
        {
            entity.SetCreationAudit(DateTime.UtcNow, "seed");
        }

        _dbContext.AddRange(hq, alex, each, box, electronics, office, laptop, paper, toner, customer1, customer2, supplier1, supplier2);

        _dbContext.ApprovalRules.AddRange(
            CreateApprovalRule("PO Manager Approval", ApprovalDocumentType.PurchaseOrder, hq.Id, 1000m, null, SystemRoles.Manager),
            CreateApprovalRule("SO Manager Approval", ApprovalDocumentType.SalesOrder, hq.Id, 1500m, null, SystemRoles.Manager),
            CreateApprovalRule("High Value Purchase Invoice", ApprovalDocumentType.PurchaseInvoice, null, 2500m, null, SystemRoles.Manager),
            CreateApprovalRule("High Value Sales Invoice", ApprovalDocumentType.SalesInvoice, null, 2500m, null, SystemRoles.Manager));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await _userManager.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await _dbContext.Branches.ToListAsync(cancellationToken);
        var hq = branches.Single(x => x.Code == "HQ");
        var alex = branches.Single(x => x.Code == "ALX");

        await CreateUserAsync("admin", "admin@erp.local", "Admin123!", SystemRoles.Admin, [hq.Id, alex.Id], hq.Id, cancellationToken);
        await CreateUserAsync("manager", "manager@erp.local", "Manager123!", SystemRoles.Manager, [hq.Id, alex.Id], hq.Id, cancellationToken);
        await CreateUserAsync("branchuser", "branch@erp.local", "Branch123!", SystemRoles.BranchUser, [hq.Id], hq.Id, cancellationToken);
    }

    private async Task SeedTransactionsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.PurchaseOrders.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hq = await _dbContext.Branches.SingleAsync(x => x.Code == "HQ", cancellationToken);
        var supplier = await _dbContext.Suppliers.SingleAsync(x => x.Code == "SUP-TECH", cancellationToken);
        var officeSupplier = await _dbContext.Suppliers.SingleAsync(x => x.Code == "SUP-OFFICE", cancellationToken);
        var customer = await _dbContext.Customers.SingleAsync(x => x.Code == "CUS-ACME", cancellationToken);
        var manager = await _userManager.Users.SingleAsync(x => x.UserName == "manager", cancellationToken);
        var laptop = await _dbContext.Products.SingleAsync(x => x.Code == "PRD-LAP", cancellationToken);
        var paper = await _dbContext.Products.SingleAsync(x => x.Code == "PRD-PAPER", cancellationToken);

        var po = new PurchaseOrder("PO-20260301-00001", supplier.Id, hq.Id, now.AddDays(-14), now.AddDays(-10), "Seeded purchase order");
        po.ReplaceLines([
            new PurchaseOrderLine(laptop.Id, 10, 600m, 0, 14, "Laptop replenishment"),
            new PurchaseOrderLine(paper.Id, 100, 4m, 0, 14, "Paper replenishment")
        ]);
        po.SetCreationAudit(now.AddDays(-14), "seed");
        po.SubmitForApproval();
        po.Approve();

        var purchaseInvoice = new PurchaseInvoice("PINV-20260302-00001", supplier.Id, hq.Id, po.Id, now.AddDays(-13), now.AddDays(17), "Received supplier invoice");
        purchaseInvoice.ReplaceLines([
            new PurchaseInvoiceLine(laptop.Id, 10, 600m, 14, po.Lines.First(x => x.ProductId == laptop.Id).Id),
            new PurchaseInvoiceLine(paper.Id, 100, 4m, 14, po.Lines.First(x => x.ProductId == paper.Id).Id)
        ]);
        purchaseInvoice.SetCreationAudit(now.AddDays(-13), "seed");
        purchaseInvoice.Post();
        po.RegisterReceipt(po.Lines.First(x => x.ProductId == laptop.Id).Id, 10);
        po.RegisterReceipt(po.Lines.First(x => x.ProductId == paper.Id).Id, 100);

        var laptopStock = new StockBalance(hq.Id, laptop.Id);
        laptopStock.SetCreationAudit(now.AddDays(-13), "seed");
        laptopStock.Receive(10, 600m);
        var paperStock = new StockBalance(hq.Id, paper.Id);
        paperStock.SetCreationAudit(now.AddDays(-13), "seed");
        paperStock.Receive(100, 4m);

        var salesOrder = new SalesOrder("SO-20260306-00001", customer.Id, hq.Id, now.AddDays(-8), now.AddDays(12), "Seeded sales order");
        salesOrder.ReplaceLines([
            new SalesOrderLine(laptop.Id, 8, 950m, 0, 14, "Laptop shipment"),
            new SalesOrderLine(paper.Id, 70, 7m, 0, 14, "Paper shipment")
        ]);
        salesOrder.SetCreationAudit(now.AddDays(-8), "seed");
        salesOrder.SubmitForApproval();
        salesOrder.Approve();

        var salesInvoice = new SalesInvoice("SINV-20260307-00001", customer.Id, hq.Id, salesOrder.Id, now.AddDays(-7), now.AddDays(23), "Seeded sales invoice");
        salesInvoice.ReplaceLines([
            new SalesInvoiceLine(laptop.Id, 8, 950m, 14, salesOrder.Lines.First(x => x.ProductId == laptop.Id).Id),
            new SalesInvoiceLine(paper.Id, 70, 7m, 14, salesOrder.Lines.First(x => x.ProductId == paper.Id).Id)
        ]);
        salesInvoice.SetCreationAudit(now.AddDays(-7), "seed");
        salesInvoice.Post();
        salesOrder.RegisterDelivery(salesOrder.Lines.First(x => x.ProductId == laptop.Id).Id, 8);
        salesOrder.RegisterDelivery(salesOrder.Lines.First(x => x.ProductId == paper.Id).Id, 70);
        laptopStock.Issue(8);
        paperStock.Issue(70);

        var supplierPayment = new Payment("PAY-20260304-00001", hq.Id, PaymentType.SupplierPayment, now.AddDays(-11), 2500m, "Bank Transfer", "SP-001", null, supplier.Id, null, purchaseInvoice.Id, "Partial supplier payment");
        supplierPayment.SetCreationAudit(now.AddDays(-11), "seed");
        purchaseInvoice.ApplyPayment(2500m);

        var customerPayment = new Payment("PAY-20260309-00002", hq.Id, PaymentType.CustomerReceipt, now.AddDays(-5), 5000m, "Cash", "CP-001", customer.Id, null, salesInvoice.Id, null, "Customer receipt");
        customerPayment.SetCreationAudit(now.AddDays(-5), "seed");
        salesInvoice.ApplyPayment(5000m);

        var salesReturn = new ReturnDocument("RET-20260310-00001", ReturnDocumentType.SalesReturn, hq.Id, now.AddDays(-3), customer.Id, null, salesInvoice.Id, null, "One laptop returned");
        salesReturn.ReplaceLines([new ReturnLine(laptop.Id, 1, 950m, "Damaged unit return")]);
        salesReturn.SetCreationAudit(now.AddDays(-3), "seed");
        salesReturn.Post();
        salesInvoice.ApplyReturn(950m);
        laptopStock.Receive(1, 600m);

        var poPending = new PurchaseOrder("PO-20260312-00002", officeSupplier.Id, hq.Id, now.AddDays(-1), now.AddDays(5), "Awaiting manager approval");
        poPending.ReplaceLines([new PurchaseOrderLine(paper.Id, 50, 4.5m, 0, 14, "Pending paper order")]);
        poPending.SetCreationAudit(now.AddDays(-1), "seed");
        poPending.SubmitForApproval();

        var approvalRule = await _dbContext.ApprovalRules.SingleAsync(x => x.Name == "PO Manager Approval", cancellationToken);
        var approvalRequest = new ApprovalRequest(approvalRule.Id, ApprovalDocumentType.PurchaseOrder, poPending.Id, hq.Id, manager.Id);
        approvalRequest.SetCreationAudit(now.AddDays(-1), "seed");

        _dbContext.AddRange(po, purchaseInvoice, salesOrder, salesInvoice, supplierPayment, customerPayment, salesReturn, poPending, approvalRequest);
        _dbContext.StockBalances.AddRange(laptopStock, paperStock);
        _dbContext.InventoryMovements.AddRange(
            new InventoryMovement(hq.Id, laptop.Id, now.AddDays(-13), InventoryMovementType.PurchaseReceipt, 10, 600m, laptopStock.QuantityOnHand + 7, 600m, purchaseInvoice.Number, nameof(PurchaseInvoice), purchaseInvoice.Id, "Seed receipt"),
            new InventoryMovement(hq.Id, paper.Id, now.AddDays(-13), InventoryMovementType.PurchaseReceipt, 100, 4m, paperStock.QuantityOnHand + 70, 4m, purchaseInvoice.Number, nameof(PurchaseInvoice), purchaseInvoice.Id, "Seed receipt"),
            new InventoryMovement(hq.Id, laptop.Id, now.AddDays(-7), InventoryMovementType.SaleIssue, -8, 600m, 2, 600m, salesInvoice.Number, nameof(SalesInvoice), salesInvoice.Id, "Seed issue"),
            new InventoryMovement(hq.Id, paper.Id, now.AddDays(-7), InventoryMovementType.SaleIssue, -70, 4m, 30, 4m, salesInvoice.Number, nameof(SalesInvoice), salesInvoice.Id, "Seed issue"),
            new InventoryMovement(hq.Id, laptop.Id, now.AddDays(-3), InventoryMovementType.SalesReturn, 1, 600m, 3, 600m, salesReturn.Number, nameof(ReturnDocument), salesReturn.Id, "Seed return"));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private ApprovalRule CreateApprovalRule(string name, ApprovalDocumentType type, Guid? branchId, decimal minimumAmount, decimal? maximumAmount, string approverRoleName)
    {
        var rule = new ApprovalRule(name, type, branchId, minimumAmount, maximumAmount, approverRoleName, null);
        rule.SetCreationAudit(DateTime.UtcNow, "seed");
        return rule;
    }

    private async Task EnsureRoleAsync(string name, string description, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        var role = await _roleManager.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (role == null)
        {
            role = new ApplicationRole { Name = name, Description = description };
            await _roleManager.CreateAsync(role);
        }

        var permissions = await _dbContext.Permissions.Where(x => permissionCodes.Contains(x.Code)).ToListAsync(cancellationToken);
        var existingRolePermissions = await _dbContext.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync(cancellationToken);
        _dbContext.RolePermissions.RemoveRange(existingRolePermissions);
        _dbContext.RolePermissions.AddRange(permissions.Select(x => new RolePermission { RoleId = role.Id, PermissionId = x.Id }));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateUserAsync(string userName, string email, string password, string roleName, IReadOnlyCollection<Guid> branchIds, Guid defaultBranchId, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            IsActive = true,
            EmailConfirmed = true,
            DefaultBranchId = defaultBranchId
        };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(user, roleName);

        foreach (var branchId in branchIds)
        {
            var access = new UserBranchAccess
            {
                UserId = user.Id,
                BranchId = branchId,
                IsDefault = branchId == defaultBranchId
            };
            access.SetCreationAudit(DateTime.UtcNow, "seed");
            _dbContext.UserBranchAccesses.Add(access);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
