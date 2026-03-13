using ERP.Domain.Entities;

namespace ERP.Application.Common.Security;

public static class PermissionCatalog
{
    public static class Auth
    {
        public const string Refresh = "AUTH.REFRESH";
    }

    public static class Branches
    {
        public const string View = "BRANCHES.VIEW";
        public const string Manage = "BRANCHES.MANAGE";
    }

    public static class Products
    {
        public const string View = "PRODUCTS.VIEW";
        public const string Manage = "PRODUCTS.MANAGE";
    }

    public static class Categories
    {
        public const string View = "CATEGORIES.VIEW";
        public const string Manage = "CATEGORIES.MANAGE";
    }

    public static class Customers
    {
        public const string View = "CUSTOMERS.VIEW";
        public const string Manage = "CUSTOMERS.MANAGE";
    }

    public static class Suppliers
    {
        public const string View = "SUPPLIERS.VIEW";
        public const string Manage = "SUPPLIERS.MANAGE";
    }

    public static class Users
    {
        public const string View = "USERS.VIEW";
        public const string Manage = "USERS.MANAGE";
    }

    public static class Roles
    {
        public const string View = "ROLES.VIEW";
        public const string Manage = "ROLES.MANAGE";
    }

    public static class PurchaseOrders
    {
        public const string View = "PO.VIEW";
        public const string Manage = "PO.MANAGE";
        public const string Approve = "PO.APPROVE";
        public const string Receive = "PO.RECEIVE";
    }

    public static class SalesOrders
    {
        public const string View = "SO.VIEW";
        public const string Manage = "SO.MANAGE";
        public const string Approve = "SO.APPROVE";
        public const string Invoice = "SO.INVOICE";
    }

    public static class Invoices
    {
        public const string View = "INVOICES.VIEW";
        public const string Manage = "INVOICES.MANAGE";
        public const string Approve = "INVOICES.APPROVE";
    }

    public static class Payments
    {
        public const string View = "PAYMENTS.VIEW";
        public const string Manage = "PAYMENTS.MANAGE";
    }

    public static class Returns
    {
        public const string View = "RETURNS.VIEW";
        public const string Manage = "RETURNS.MANAGE";
    }

    public static class Inventory
    {
        public const string View = "INVENTORY.VIEW";
        public const string Adjust = "INVENTORY.ADJUST";
        public const string Transfer = "INVENTORY.TRANSFER";
    }

    public static class Approvals
    {
        public const string View = "APPROVALS.VIEW";
        public const string ManageRules = "APPROVALS.RULES.MANAGE";
        public const string Act = "APPROVALS.ACT";
    }

    public static class Reports
    {
        public const string View = "REPORTS.VIEW";
        public const string Export = "REPORTS.EXPORT";
    }

    public static class Dashboard
    {
        public const string View = "DASHBOARD.VIEW";
    }

    public static class AuditLogs
    {
        public const string View = "AUDIT.VIEW";
    }

    public static class Alerts
    {
        public const string View = "ALERTS.VIEW";
        public const string Manage = "ALERTS.MANAGE";
    }

    public static IReadOnlyCollection<Permission> GetAll() =>
    [
        Create("Security", Branches.View, "View branches"),
        Create("Security", Branches.Manage, "Manage branches"),
        Create("MasterData", Products.View, "View products"),
        Create("MasterData", Products.Manage, "Manage products"),
        Create("MasterData", Categories.View, "View categories"),
        Create("MasterData", Categories.Manage, "Manage categories"),
        Create("MasterData", Customers.View, "View customers"),
        Create("MasterData", Customers.Manage, "Manage customers"),
        Create("MasterData", Suppliers.View, "View suppliers"),
        Create("MasterData", Suppliers.Manage, "Manage suppliers"),
        Create("Admin", Users.View, "View users"),
        Create("Admin", Users.Manage, "Manage users"),
        Create("Admin", Roles.View, "View roles"),
        Create("Admin", Roles.Manage, "Manage roles"),
        Create("Purchasing", PurchaseOrders.View, "View purchase orders"),
        Create("Purchasing", PurchaseOrders.Manage, "Manage purchase orders"),
        Create("Purchasing", PurchaseOrders.Approve, "Approve purchase orders"),
        Create("Purchasing", PurchaseOrders.Receive, "Receive purchase stock"),
        Create("Sales", SalesOrders.View, "View sales orders"),
        Create("Sales", SalesOrders.Manage, "Manage sales orders"),
        Create("Sales", SalesOrders.Approve, "Approve sales orders"),
        Create("Sales", SalesOrders.Invoice, "Create sales invoices"),
        Create("Finance", Invoices.View, "View invoices"),
        Create("Finance", Invoices.Manage, "Manage invoices"),
        Create("Finance", Invoices.Approve, "Approve invoices"),
        Create("Finance", Payments.View, "View payments"),
        Create("Finance", Payments.Manage, "Manage payments"),
        Create("Inventory", Inventory.View, "View inventory"),
        Create("Inventory", Inventory.Adjust, "Adjust inventory"),
        Create("Inventory", Inventory.Transfer, "Transfer inventory"),
        Create("Inventory", Returns.View, "View returns"),
        Create("Inventory", Returns.Manage, "Manage returns"),
        Create("Workflow", Approvals.View, "View approvals"),
        Create("Workflow", Approvals.ManageRules, "Manage approval rules"),
        Create("Workflow", Approvals.Act, "Approve and reject documents"),
        Create("Reporting", Reports.View, "View reports"),
        Create("Reporting", Reports.Export, "Export reports"),
        Create("Reporting", Dashboard.View, "View dashboard"),
        Create("Admin", AuditLogs.View, "View audit logs"),
        Create("Alerts", Alerts.View, "View alerts"),
        Create("Alerts", Alerts.Manage, "Manage alerts")
    ];

    private static Permission Create(string module, string code, string name) =>
        new(module, code, name, $"Permission for {name.ToLowerInvariant()}");
}
