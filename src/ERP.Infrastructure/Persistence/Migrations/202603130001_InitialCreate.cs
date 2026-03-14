using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("202603130001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE [Roles] (
                [Id] uniqueidentifier NOT NULL,
                [Name] nvarchar(256) NULL,
                [NormalizedName] nvarchar(256) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                [Description] nvarchar(256) NULL,
                CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Users] (
                [Id] uniqueidentifier NOT NULL,
                [UserName] nvarchar(256) NULL,
                [NormalizedUserName] nvarchar(256) NULL,
                [Email] nvarchar(256) NULL,
                [NormalizedEmail] nvarchar(256) NULL,
                [EmailConfirmed] bit NOT NULL,
                [PasswordHash] nvarchar(max) NULL,
                [SecurityStamp] nvarchar(max) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                [PhoneNumber] nvarchar(max) NULL,
                [PhoneNumberConfirmed] bit NOT NULL,
                [TwoFactorEnabled] bit NOT NULL,
                [LockoutEnd] datetimeoffset NULL,
                [LockoutEnabled] bit NOT NULL,
                [AccessFailedCount] int NOT NULL,
                [IsActive] bit NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT CAST(1 AS bit),
                [DefaultBranchId] uniqueidentifier NULL,
                CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
            );

            CREATE TABLE [RoleClaims] (
                [Id] int NOT NULL IDENTITY(1,1),
                [RoleId] uniqueidentifier NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [UserClaims] (
                [Id] int NOT NULL IDENTITY(1,1),
                [UserId] uniqueidentifier NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [UserLogins] (
                [LoginProvider] nvarchar(450) NOT NULL,
                [ProviderKey] nvarchar(450) NOT NULL,
                [ProviderDisplayName] nvarchar(max) NULL,
                [UserId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
                CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [UserRoles] (
                [UserId] uniqueidentifier NOT NULL,
                [RoleId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
                CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [UserTokens] (
                [UserId] uniqueidentifier NOT NULL,
                [LoginProvider] nvarchar(450) NOT NULL,
                [Name] nvarchar(450) NOT NULL,
                [Value] nvarchar(max) NULL,
                CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
                CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [Branches] (
                [Id] uniqueidentifier NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [Address] nvarchar(max) NULL,
                [Phone] nvarchar(max) NULL,
                [Email] nvarchar(max) NULL,
                [IsActive] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Branches_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Customers] (
                [Id] uniqueidentifier NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [TaxNumber] nvarchar(max) NULL,
                [Email] nvarchar(128) NULL,
                [Phone] nvarchar(32) NULL,
                [Address] nvarchar(256) NULL,
                [CreditLimit] decimal(18,4) NOT NULL,
                [PaymentTermsDays] int NOT NULL,
                [IsActive] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Customers_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
            );

            CREATE TABLE [NumberSequences] (
                [Id] uniqueidentifier NOT NULL,
                [Prefix] nvarchar(32) NOT NULL,
                [CurrentValue] int NOT NULL,
                [LastUpdatedUtc] datetime2 NOT NULL,
                CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Permissions] (
                [Id] uniqueidentifier NOT NULL,
                [Module] nvarchar(64) NOT NULL,
                [Code] nvarchar(64) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [Description] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Permissions_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
            );

            CREATE TABLE [ProductCategories] (
                [Id] uniqueidentifier NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [Description] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_ProductCategories_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Suppliers] (
                [Id] uniqueidentifier NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [TaxNumber] nvarchar(max) NULL,
                [Email] nvarchar(128) NULL,
                [Phone] nvarchar(32) NULL,
                [Address] nvarchar(256) NULL,
                [PaymentTermsDays] int NOT NULL,
                [IsActive] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Suppliers_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
            );

            CREATE TABLE [UnitsOfMeasure] (
                [Id] uniqueidentifier NOT NULL,
                [Name] nvarchar(64) NOT NULL,
                [Symbol] nvarchar(16) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_UnitsOfMeasure_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_UnitsOfMeasure] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Products] (
                [Id] uniqueidentifier NOT NULL,
                [Code] nvarchar(32) NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [SKU] nvarchar(64) NOT NULL,
                [Description] nvarchar(512) NULL,
                [CategoryId] uniqueidentifier NOT NULL,
                [UnitOfMeasureId] uniqueidentifier NULL,
                [ReorderLevel] decimal(18,4) NOT NULL,
                [StandardCost] decimal(18,4) NOT NULL,
                [SalePrice] decimal(18,4) NOT NULL,
                [IsStockTracked] bit NOT NULL,
                [IsActive] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Products_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]),
                CONSTRAINT [FK_Products_UnitsOfMeasure_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [UnitsOfMeasure] ([Id])
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
            CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
            CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
            CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
            CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
            CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
            CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
            CREATE UNIQUE INDEX [IX_Branches_Code] ON [Branches] ([Code]);
            CREATE UNIQUE INDEX [IX_Customers_Code] ON [Customers] ([Code]);
            CREATE UNIQUE INDEX [IX_NumberSequences_Prefix] ON [NumberSequences] ([Prefix]);
            CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]);
            CREATE UNIQUE INDEX [IX_ProductCategories_Code] ON [ProductCategories] ([Code]);
            CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
            CREATE UNIQUE INDEX [IX_Products_Code] ON [Products] ([Code]);
            CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]);
            CREATE INDEX [IX_Products_UnitOfMeasureId] ON [Products] ([UnitOfMeasureId]);
            CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]);
            CREATE UNIQUE INDEX [IX_UnitsOfMeasure_Symbol] ON [UnitsOfMeasure] ([Symbol]);
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE [RefreshTokens] (
                [Id] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [Token] nvarchar(256) NOT NULL,
                [ExpiresAtUtc] datetime2 NOT NULL,
                [RevokedAtUtc] datetime2 NULL,
                [CreatedByIp] nvarchar(64) NOT NULL,
                [ReplacedByToken] nvarchar(max) NULL,
                [UserAgent] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_RefreshTokens_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [RolePermissions] (
                [RoleId] uniqueidentifier NOT NULL,
                [PermissionId] uniqueidentifier NOT NULL,
                CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
                CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [UserBranchAccesses] (
                [Id] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [IsDefault] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_UserBranchAccesses_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_UserBranchAccesses] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_UserBranchAccesses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_UserBranchAccesses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [ApprovalRules] (
                [Id] uniqueidentifier NOT NULL,
                [Name] nvarchar(128) NOT NULL,
                [DocumentType] int NOT NULL,
                [BranchId] uniqueidentifier NULL,
                [MinimumAmount] decimal(18,4) NOT NULL,
                [MaximumAmount] decimal(18,4) NULL,
                [ApproverRoleName] nvarchar(64) NULL,
                [ApproverUserId] uniqueidentifier NULL,
                [IsActive] bit NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_ApprovalRules_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_ApprovalRules] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ApprovalRules_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id])
            );

            CREATE TABLE [PurchaseOrders] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [SupplierId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [OrderDateUtc] datetime2 NOT NULL,
                [ExpectedDateUtc] datetime2 NULL,
                [Notes] nvarchar(512) NULL,
                [Status] int NOT NULL,
                [TotalAmount] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_PurchaseOrders_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PurchaseOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
            );

            CREATE TABLE [PurchaseOrderLines] (
                [Id] uniqueidentifier NOT NULL,
                [PurchaseOrderId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [OrderedQuantity] decimal(18,4) NOT NULL,
                [ReceivedQuantity] decimal(18,4) NOT NULL,
                [UnitPrice] decimal(18,4) NOT NULL,
                [DiscountPercent] decimal(18,4) NOT NULL,
                [TaxPercent] decimal(18,4) NOT NULL,
                [LineTotal] decimal(18,4) NOT NULL,
                [Description] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_PurchaseOrderLines_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PurchaseOrderLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
                CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [PurchaseInvoices] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [SupplierId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [PurchaseOrderId] uniqueidentifier NULL,
                [InvoiceDateUtc] datetime2 NOT NULL,
                [DueDateUtc] datetime2 NOT NULL,
                [Notes] nvarchar(512) NULL,
                [Status] int NOT NULL,
                [TotalAmount] decimal(18,4) NOT NULL,
                [PaidAmount] decimal(18,4) NOT NULL,
                [ReturnAmount] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_PurchaseInvoices_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_PurchaseInvoices] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PurchaseInvoices_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]),
                CONSTRAINT [FK_PurchaseInvoices_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
            );

            CREATE TABLE [PurchaseInvoiceLines] (
                [Id] uniqueidentifier NOT NULL,
                [PurchaseInvoiceId] uniqueidentifier NOT NULL,
                [PurchaseOrderLineId] uniqueidentifier NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [Quantity] decimal(18,4) NOT NULL,
                [UnitPrice] decimal(18,4) NOT NULL,
                [TaxPercent] decimal(18,4) NOT NULL,
                [LineTotal] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_PurchaseInvoiceLines_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_PurchaseInvoiceLines] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PurchaseInvoiceLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
                CONSTRAINT [FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [PurchaseInvoices] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_PurchaseInvoiceLines_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [PurchaseOrderLines] ([Id])
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
            CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
            CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
            CREATE INDEX [IX_UserBranchAccesses_BranchId] ON [UserBranchAccesses] ([BranchId]);
            CREATE UNIQUE INDEX [IX_UserBranchAccesses_UserId_BranchId] ON [UserBranchAccesses] ([UserId], [BranchId]);
            CREATE INDEX [IX_ApprovalRules_BranchId] ON [ApprovalRules] ([BranchId]);
            CREATE INDEX [IX_PurchaseOrders_BranchId] ON [PurchaseOrders] ([BranchId]);
            CREATE UNIQUE INDEX [IX_PurchaseOrders_Number] ON [PurchaseOrders] ([Number]);
            CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
            CREATE INDEX [IX_PurchaseOrderLines_ProductId] ON [PurchaseOrderLines] ([ProductId]);
            CREATE INDEX [IX_PurchaseOrderLines_PurchaseOrderId] ON [PurchaseOrderLines] ([PurchaseOrderId]);
            CREATE INDEX [IX_PurchaseInvoices_BranchId] ON [PurchaseInvoices] ([BranchId]);
            CREATE UNIQUE INDEX [IX_PurchaseInvoices_Number] ON [PurchaseInvoices] ([Number]);
            CREATE INDEX [IX_PurchaseInvoices_PurchaseOrderId] ON [PurchaseInvoices] ([PurchaseOrderId]);
            CREATE INDEX [IX_PurchaseInvoices_SupplierId] ON [PurchaseInvoices] ([SupplierId]);
            CREATE INDEX [IX_PurchaseInvoiceLines_ProductId] ON [PurchaseInvoiceLines] ([ProductId]);
            CREATE INDEX [IX_PurchaseInvoiceLines_PurchaseInvoiceId] ON [PurchaseInvoiceLines] ([PurchaseInvoiceId]);
            CREATE INDEX [IX_PurchaseInvoiceLines_PurchaseOrderLineId] ON [PurchaseInvoiceLines] ([PurchaseOrderLineId]);
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE [SalesOrders] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [CustomerId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [OrderDateUtc] datetime2 NOT NULL,
                [DueDateUtc] datetime2 NULL,
                [Notes] nvarchar(512) NULL,
                [Status] int NOT NULL,
                [TotalAmount] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_SalesOrders_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_SalesOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id])
            );

            CREATE TABLE [SalesOrderLines] (
                [Id] uniqueidentifier NOT NULL,
                [SalesOrderId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [OrderedQuantity] decimal(18,4) NOT NULL,
                [DeliveredQuantity] decimal(18,4) NOT NULL,
                [UnitPrice] decimal(18,4) NOT NULL,
                [DiscountPercent] decimal(18,4) NOT NULL,
                [TaxPercent] decimal(18,4) NOT NULL,
                [LineTotal] decimal(18,4) NOT NULL,
                [Description] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_SalesOrderLines_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_SalesOrderLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
                CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [SalesInvoices] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [CustomerId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [SalesOrderId] uniqueidentifier NULL,
                [InvoiceDateUtc] datetime2 NOT NULL,
                [DueDateUtc] datetime2 NOT NULL,
                [Notes] nvarchar(512) NULL,
                [Status] int NOT NULL,
                [TotalAmount] decimal(18,4) NOT NULL,
                [PaidAmount] decimal(18,4) NOT NULL,
                [ReturnAmount] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_SalesInvoices_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_SalesInvoices] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_SalesInvoices_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_SalesInvoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
                CONSTRAINT [FK_SalesInvoices_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id])
            );

            CREATE TABLE [SalesInvoiceLines] (
                [Id] uniqueidentifier NOT NULL,
                [SalesInvoiceId] uniqueidentifier NOT NULL,
                [SalesOrderLineId] uniqueidentifier NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [Quantity] decimal(18,4) NOT NULL,
                [UnitPrice] decimal(18,4) NOT NULL,
                [TaxPercent] decimal(18,4) NOT NULL,
                [LineTotal] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_SalesInvoiceLines_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_SalesInvoiceLines] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_SalesInvoiceLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
                CONSTRAINT [FK_SalesInvoiceLines_SalesOrderLines_SalesOrderLineId] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [SalesOrderLines] ([Id]),
                CONSTRAINT [FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [SalesInvoices] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [Payments] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [Type] int NOT NULL,
                [PaymentDateUtc] datetime2 NOT NULL,
                [Amount] decimal(18,4) NOT NULL,
                [Method] nvarchar(64) NOT NULL,
                [ReferenceNumber] nvarchar(64) NULL,
                [CustomerId] uniqueidentifier NULL,
                [SupplierId] uniqueidentifier NULL,
                [SalesInvoiceId] uniqueidentifier NULL,
                [PurchaseInvoiceId] uniqueidentifier NULL,
                [Notes] nvarchar(256) NULL,
                [Status] int NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Payments_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Payments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
                CONSTRAINT [FK_Payments_PurchaseInvoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [PurchaseInvoices] ([Id]),
                CONSTRAINT [FK_Payments_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [SalesInvoices] ([Id]),
                CONSTRAINT [FK_Payments_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
            );

            CREATE TABLE [ReturnDocuments] (
                [Id] uniqueidentifier NOT NULL,
                [Number] nvarchar(32) NOT NULL,
                [Type] int NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [ReturnDateUtc] datetime2 NOT NULL,
                [CustomerId] uniqueidentifier NULL,
                [SupplierId] uniqueidentifier NULL,
                [SalesInvoiceId] uniqueidentifier NULL,
                [PurchaseInvoiceId] uniqueidentifier NULL,
                [Reason] nvarchar(256) NULL,
                [Status] int NOT NULL,
                [TotalAmount] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_ReturnDocuments_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_ReturnDocuments] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ReturnDocuments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_ReturnDocuments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
                CONSTRAINT [FK_ReturnDocuments_PurchaseInvoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [PurchaseInvoices] ([Id]),
                CONSTRAINT [FK_ReturnDocuments_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [SalesInvoices] ([Id]),
                CONSTRAINT [FK_ReturnDocuments_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
            );

            CREATE TABLE [ReturnLines] (
                [Id] uniqueidentifier NOT NULL,
                [ReturnDocumentId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [Quantity] decimal(18,4) NOT NULL,
                [UnitPrice] decimal(18,4) NOT NULL,
                [LineTotal] decimal(18,4) NOT NULL,
                [Reason] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_ReturnLines_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_ReturnLines] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ReturnLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
                CONSTRAINT [FK_ReturnLines_ReturnDocuments_ReturnDocumentId] FOREIGN KEY ([ReturnDocumentId]) REFERENCES [ReturnDocuments] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [InventoryMovements] (
                [Id] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [MovementDateUtc] datetime2 NOT NULL,
                [Type] int NOT NULL,
                [Quantity] decimal(18,4) NOT NULL,
                [UnitCost] decimal(18,4) NOT NULL,
                [QuantityAfter] decimal(18,4) NOT NULL,
                [AverageCostAfter] decimal(18,4) NOT NULL,
                [ReferenceNumber] nvarchar(32) NOT NULL,
                [ReferenceDocumentType] nvarchar(64) NULL,
                [ReferenceDocumentId] uniqueidentifier NULL,
                [Remarks] nvarchar(256) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_InventoryMovements_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_InventoryMovements_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_InventoryMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
            );

            CREATE TABLE [StockBalances] (
                [Id] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NOT NULL,
                [QuantityOnHand] decimal(18,4) NOT NULL,
                [ReservedQuantity] decimal(18,4) NOT NULL,
                [AverageCost] decimal(18,4) NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_StockBalances_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_StockBalances] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_StockBalances_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_StockBalances_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
            );

            CREATE TABLE [ApprovalRequests] (
                [Id] uniqueidentifier NOT NULL,
                [RuleId] uniqueidentifier NOT NULL,
                [DocumentType] int NOT NULL,
                [DocumentId] uniqueidentifier NOT NULL,
                [BranchId] uniqueidentifier NULL,
                [RequestedByUserId] uniqueidentifier NOT NULL,
                [ReviewedByUserId] uniqueidentifier NULL,
                [Status] int NOT NULL,
                [RequestedAtUtc] datetime2 NOT NULL,
                [ReviewedAtUtc] datetime2 NULL,
                [Comments] nvarchar(512) NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_ApprovalRequests_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_ApprovalRequests] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ApprovalRequests_ApprovalRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [ApprovalRules] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [AuditLogs] (
                [Id] uniqueidentifier NOT NULL,
                [EntityName] nvarchar(128) NOT NULL,
                [EntityId] nvarchar(64) NOT NULL,
                [Action] nvarchar(64) NOT NULL,
                [BeforeData] nvarchar(max) NULL,
                [AfterData] nvarchar(max) NULL,
                [PerformedByUserId] uniqueidentifier NULL,
                [UserName] nvarchar(128) NULL,
                [BranchId] uniqueidentifier NULL,
                [IpAddress] nvarchar(64) NULL,
                [TimestampUtc] datetime2 NOT NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Alerts] (
                [Id] uniqueidentifier NOT NULL,
                [Type] int NOT NULL,
                [BranchId] uniqueidentifier NOT NULL,
                [ProductId] uniqueidentifier NULL,
                [Title] nvarchar(128) NOT NULL,
                [Message] nvarchar(512) NOT NULL,
                [IsRead] bit NOT NULL,
                [IsActive] bit NOT NULL,
                [TriggeredAtUtc] datetime2 NOT NULL,
                [ResolvedAtUtc] datetime2 NULL,
                [IsDeleted] bit NOT NULL CONSTRAINT [DF_Alerts_IsDeleted] DEFAULT CAST(0 AS bit),
                [CreatedAtUtc] datetime2 NOT NULL,
                [CreatedBy] nvarchar(128) NULL,
                [UpdatedAtUtc] datetime2 NULL,
                [UpdatedBy] nvarchar(128) NULL,
                [RowVersion] rowversion NOT NULL,
                CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Alerts_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
                CONSTRAINT [FK_Alerts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX [IX_SalesOrders_BranchId] ON [SalesOrders] ([BranchId]);
            CREATE INDEX [IX_SalesOrders_CustomerId] ON [SalesOrders] ([CustomerId]);
            CREATE UNIQUE INDEX [IX_SalesOrders_Number] ON [SalesOrders] ([Number]);
            CREATE INDEX [IX_SalesOrderLines_ProductId] ON [SalesOrderLines] ([ProductId]);
            CREATE INDEX [IX_SalesOrderLines_SalesOrderId] ON [SalesOrderLines] ([SalesOrderId]);
            CREATE INDEX [IX_SalesInvoices_BranchId] ON [SalesInvoices] ([BranchId]);
            CREATE INDEX [IX_SalesInvoices_CustomerId] ON [SalesInvoices] ([CustomerId]);
            CREATE UNIQUE INDEX [IX_SalesInvoices_Number] ON [SalesInvoices] ([Number]);
            CREATE INDEX [IX_SalesInvoices_SalesOrderId] ON [SalesInvoices] ([SalesOrderId]);
            CREATE INDEX [IX_SalesInvoiceLines_ProductId] ON [SalesInvoiceLines] ([ProductId]);
            CREATE INDEX [IX_SalesInvoiceLines_SalesInvoiceId] ON [SalesInvoiceLines] ([SalesInvoiceId]);
            CREATE INDEX [IX_SalesInvoiceLines_SalesOrderLineId] ON [SalesInvoiceLines] ([SalesOrderLineId]);
            CREATE INDEX [IX_Payments_BranchId] ON [Payments] ([BranchId]);
            CREATE INDEX [IX_Payments_CustomerId] ON [Payments] ([CustomerId]);
            CREATE UNIQUE INDEX [IX_Payments_Number] ON [Payments] ([Number]);
            CREATE INDEX [IX_Payments_PurchaseInvoiceId] ON [Payments] ([PurchaseInvoiceId]);
            CREATE INDEX [IX_Payments_SalesInvoiceId] ON [Payments] ([SalesInvoiceId]);
            CREATE INDEX [IX_Payments_SupplierId] ON [Payments] ([SupplierId]);
            CREATE INDEX [IX_ReturnDocuments_BranchId] ON [ReturnDocuments] ([BranchId]);
            CREATE INDEX [IX_ReturnDocuments_CustomerId] ON [ReturnDocuments] ([CustomerId]);
            CREATE UNIQUE INDEX [IX_ReturnDocuments_Number] ON [ReturnDocuments] ([Number]);
            CREATE INDEX [IX_ReturnDocuments_PurchaseInvoiceId] ON [ReturnDocuments] ([PurchaseInvoiceId]);
            CREATE INDEX [IX_ReturnDocuments_SalesInvoiceId] ON [ReturnDocuments] ([SalesInvoiceId]);
            CREATE INDEX [IX_ReturnDocuments_SupplierId] ON [ReturnDocuments] ([SupplierId]);
            CREATE INDEX [IX_ReturnLines_ProductId] ON [ReturnLines] ([ProductId]);
            CREATE INDEX [IX_ReturnLines_ReturnDocumentId] ON [ReturnLines] ([ReturnDocumentId]);
            CREATE INDEX [IX_InventoryMovements_BranchId] ON [InventoryMovements] ([BranchId]);
            CREATE INDEX [IX_InventoryMovements_ProductId] ON [InventoryMovements] ([ProductId]);
            CREATE UNIQUE INDEX [IX_StockBalances_BranchId_ProductId] ON [StockBalances] ([BranchId], [ProductId]);
            CREATE INDEX [IX_StockBalances_ProductId] ON [StockBalances] ([ProductId]);
            CREATE INDEX [IX_ApprovalRequests_RuleId] ON [ApprovalRequests] ([RuleId]);
            CREATE INDEX [IX_Alerts_BranchId] ON [Alerts] ([BranchId]);
            CREATE INDEX [IX_Alerts_ProductId] ON [Alerts] ([ProductId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS [Alerts];
            DROP TABLE IF EXISTS [AuditLogs];
            DROP TABLE IF EXISTS [ApprovalRequests];
            DROP TABLE IF EXISTS [StockBalances];
            DROP TABLE IF EXISTS [InventoryMovements];
            DROP TABLE IF EXISTS [ReturnLines];
            DROP TABLE IF EXISTS [ReturnDocuments];
            DROP TABLE IF EXISTS [Payments];
            DROP TABLE IF EXISTS [SalesInvoiceLines];
            DROP TABLE IF EXISTS [SalesInvoices];
            DROP TABLE IF EXISTS [SalesOrderLines];
            DROP TABLE IF EXISTS [SalesOrders];
            DROP TABLE IF EXISTS [PurchaseInvoiceLines];
            DROP TABLE IF EXISTS [PurchaseInvoices];
            DROP TABLE IF EXISTS [PurchaseOrderLines];
            DROP TABLE IF EXISTS [PurchaseOrders];
            DROP TABLE IF EXISTS [ApprovalRules];
            DROP TABLE IF EXISTS [UserBranchAccesses];
            DROP TABLE IF EXISTS [RolePermissions];
            DROP TABLE IF EXISTS [RefreshTokens];
            DROP TABLE IF EXISTS [Products];
            DROP TABLE IF EXISTS [UnitsOfMeasure];
            DROP TABLE IF EXISTS [Suppliers];
            DROP TABLE IF EXISTS [ProductCategories];
            DROP TABLE IF EXISTS [Permissions];
            DROP TABLE IF EXISTS [NumberSequences];
            DROP TABLE IF EXISTS [Customers];
            DROP TABLE IF EXISTS [Branches];
            DROP TABLE IF EXISTS [UserTokens];
            DROP TABLE IF EXISTS [UserRoles];
            DROP TABLE IF EXISTS [UserLogins];
            DROP TABLE IF EXISTS [UserClaims];
            DROP TABLE IF EXISTS [RoleClaims];
            DROP TABLE IF EXISTS [Users];
            DROP TABLE IF EXISTS [Roles];
            """);
    }
}
