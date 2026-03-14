using System.Net.Http.Json;
using ERP.Application.Approvals;
using ERP.Application.Auth;
using ERP.Application.Common.Models;
using ERP.Application.Dashboard;
using ERP.Application.Inventory;
using ERP.Application.MasterData;
using ERP.Application.Purchasing;
using ERP.Application.Sales;
using ERP.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests;

public sealed class CriticalFlowsTests : IClassFixture<ErpWebApplicationFactory>
{
    private readonly ErpWebApplicationFactory _factory;

    public CriticalFlowsTests(ErpWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_Return_Access_And_Refresh_Tokens()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UserNameOrEmail = "manager",
            Password = "Manager123!"
        });

        var envelope = await response.ReadAsAsync<TokenEnvelope>();
        envelope.AccessToken.Should().NotBeNullOrWhiteSpace();
        envelope.RefreshToken.Should().NotBeNullOrWhiteSpace();
        envelope.User.UserName.Should().Be("manager");
        envelope.User.Roles.Should().Contain("Manager");
        envelope.User.BranchIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Authenticated_Surface_EndPoints_Should_Respond_For_Frontend_Flows()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync("admin", "Admin123!");

        var endpoints = new[]
        {
            "/api/v1/auth/me",
            "/api/v1/dashboard",
            "/api/v1/branches?pageNumber=1&pageSize=10",
            "/api/v1/categories?pageNumber=1&pageSize=10",
            "/api/v1/products?pageNumber=1&pageSize=10",
            "/api/v1/customers?pageNumber=1&pageSize=10",
            "/api/v1/suppliers?pageNumber=1&pageSize=10",
            "/api/v1/purchase-orders?pageNumber=1&pageSize=10",
            "/api/v1/sales-orders?pageNumber=1&pageSize=10",
            "/api/v1/invoices/purchase?pageNumber=1&pageSize=10",
            "/api/v1/invoices/sales?pageNumber=1&pageSize=10",
            "/api/v1/payments?pageNumber=1&pageSize=10",
            "/api/v1/returns?pageNumber=1&pageSize=10",
            "/api/v1/approvals/requests?pageNumber=1&pageSize=10",
            "/api/v1/approvals/rules?pageNumber=1&pageSize=10",
            "/api/v1/users?pageNumber=1&pageSize=10",
            "/api/v1/roles",
            "/api/v1/permissions",
            "/api/v1/alerts?pageNumber=1&pageSize=10",
            "/api/v1/audit-logs?pageNumber=1&pageSize=10",
            "/api/v1/reports/sales-summary",
            "/api/v1/reports/purchase-summary",
            "/api/v1/reports/inventory-valuation",
            "/api/v1/reports/stock-movement",
            "/api/v1/reports/low-stock",
            "/api/v1/reports/receivables",
            "/api/v1/reports/payables"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            await response.EnsureSuccessWithBodyAsync();
        }
    }

    [Fact]
    public async Task Create_Product_Should_Persist_And_Be_Queryable()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await GetLookupByCodeAsync(client, "/api/v1/categories/lookup", "OFF");

        var request = new SaveProductRequest
        {
            Code = $"PRD-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Name = "Integration Test Product",
            SKU = $"SKU-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            CategoryId = category.Id,
            ReorderLevel = 3,
            StandardCost = 12.5m,
            SalePrice = 18m,
            IsStockTracked = true,
            IsActive = true,
            Description = "Created by integration test"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", request);
        var productId = await createResponse.ReadAsAsync<Guid>();

        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        var product = await getResponse.ReadAsAsync<ProductDto>();

        product.Code.Should().Be(request.Code);
        product.Name.Should().Be(request.Name);
        product.SKU.Should().Be(request.SKU);
        product.CategoryId.Should().Be(category.Id);
        product.StandardCost.Should().Be(request.StandardCost);
        product.SalePrice.Should().Be(request.SalePrice);
    }

    [Fact]
    public async Task Procurement_And_Sales_Flow_Should_Post_Stock_And_Generate_Low_Stock_Alert()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var branch = await GetLookupByCodeAsync(client, "/api/v1/branches/lookup", "HQ");
        var customer = await GetLookupByCodeAsync(client, "/api/v1/customers/lookup", "CUS-ACME");
        var supplier = await GetLookupByCodeAsync(client, "/api/v1/suppliers/lookup", "SUP-OFFICE");
        var toner = await GetLookupByCodeAsync(client, "/api/v1/products/lookup", "PRD-TONER");

        var purchaseOrderId = await CreatePurchaseOrderAsync(client, branch.Id, supplier.Id, toner.Id);
        await SubmitDocumentAsync(client, $"/api/v1/purchase-orders/{purchaseOrderId}/submit");
        await ApproveRequestAsync(client, ApprovalDocumentType.PurchaseOrder, purchaseOrderId);

        var purchaseOrder = await GetPurchaseOrderAsync(client, purchaseOrderId);
        purchaseOrder.Status.Should().Be(PurchaseOrderStatus.Approved);

        var purchaseInvoiceId = await CreatePurchaseInvoiceAsync(client, branch.Id, supplier.Id, purchaseOrder);
        var purchaseInvoice = await GetPurchaseInvoiceAsync(client, purchaseInvoiceId);
        purchaseInvoice.Status.Should().Be(InvoiceStatus.Posted);

        var stockAfterReceipt = await GetStockBalanceAsync(client, branch.Id, toner.Id);
        stockAfterReceipt.QuantityOnHand.Should().Be(24m);

        var salesOrderId = await CreateSalesOrderAsync(client, branch.Id, customer.Id, toner.Id);
        await SubmitDocumentAsync(client, $"/api/v1/sales-orders/{salesOrderId}/submit");
        await ApproveRequestAsync(client, ApprovalDocumentType.SalesOrder, salesOrderId);

        var salesOrder = await GetSalesOrderAsync(client, salesOrderId);
        salesOrder.Status.Should().Be(SalesOrderStatus.Approved);

        var salesInvoiceId = await CreateSalesInvoiceAsync(client, branch.Id, customer.Id, salesOrder);
        var salesInvoice = await GetSalesInvoiceAsync(client, salesInvoiceId);
        salesInvoice.Status.Should().Be(InvoiceStatus.Posted);

        var stockAfterIssue = await GetStockBalanceAsync(client, branch.Id, toner.Id);
        stockAfterIssue.QuantityOnHand.Should().Be(4m);
        stockAfterIssue.IsLowStock.Should().BeTrue();

        await _factory.RunLowStockJobAsync();

        var alerts = await GetPagedAsync<AlertDto>(client, $"/api/v1/alerts?pageNumber=1&pageSize=50&branchId={branch.Id}");
        alerts.Items.Should().Contain(x => x.Title.Contains("Laser Toner Cartridge", StringComparison.OrdinalIgnoreCase));

        var dashboardResponse = await client.GetAsync($"/api/v1/dashboard?branchId={branch.Id}");
        var dashboard = await dashboardResponse.ReadAsAsync<DashboardDto>();
        dashboard.LowStockCount.Should().BeGreaterThan(0);
        dashboard.LowStockAlerts.Should().Contain(x => x.Title.Contains("Laser Toner Cartridge", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task SubmitDocumentAsync(HttpClient client, string path)
    {
        var response = await client.PostAsJsonAsync(path, new { });
        await response.EnsureSuccessWithBodyAsync();
    }

    private static async Task<Guid> CreatePurchaseOrderAsync(HttpClient client, Guid branchId, Guid supplierId, Guid productId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/purchase-orders", new SavePurchaseOrderRequest
        {
            BranchId = branchId,
            SupplierId = supplierId,
            OrderDateUtc = DateTime.UtcNow,
            ExpectedDateUtc = DateTime.UtcNow.AddDays(5),
            Notes = "Integration purchase order",
            Lines =
            [
                new SavePurchaseOrderLineRequest
                {
                    ProductId = productId,
                    Quantity = 24m,
                    UnitPrice = 45m,
                    DiscountPercent = 0m,
                    TaxPercent = 14m,
                    Description = "Procurement test line"
                }
            ]
        });

        return await response.ReadAsAsync<Guid>();
    }

    private static async Task<Guid> CreatePurchaseInvoiceAsync(HttpClient client, Guid branchId, Guid supplierId, PurchaseOrderDto purchaseOrder)
    {
        var line = purchaseOrder.Lines.Single();
        var response = await client.PostAsJsonAsync("/api/v1/invoices/purchase", new SavePurchaseInvoiceRequest
        {
            BranchId = branchId,
            SupplierId = supplierId,
            PurchaseOrderId = purchaseOrder.Id,
            InvoiceDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            Notes = "Integration purchase invoice",
            Lines =
            [
                new SavePurchaseInvoiceLineRequest
                {
                    ProductId = line.ProductId,
                    Quantity = 24m,
                    UnitPrice = 45m,
                    TaxPercent = 14m,
                    PurchaseOrderLineId = line.Id
                }
            ]
        });

        return await response.ReadAsAsync<Guid>();
    }

    private static async Task<Guid> CreateSalesOrderAsync(HttpClient client, Guid branchId, Guid customerId, Guid productId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/sales-orders", new SaveSalesOrderRequest
        {
            BranchId = branchId,
            CustomerId = customerId,
            OrderDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow.AddDays(10),
            Notes = "Integration sales order",
            Lines =
            [
                new SaveSalesOrderLineRequest
                {
                    ProductId = productId,
                    Quantity = 20m,
                    UnitPrice = 75m,
                    DiscountPercent = 0m,
                    TaxPercent = 14m,
                    Description = "Sales test line"
                }
            ]
        });

        return await response.ReadAsAsync<Guid>();
    }

    private static async Task<Guid> CreateSalesInvoiceAsync(HttpClient client, Guid branchId, Guid customerId, SalesOrderDto salesOrder)
    {
        var line = salesOrder.Lines.Single();
        var response = await client.PostAsJsonAsync("/api/v1/invoices/sales", new SaveSalesInvoiceRequest
        {
            BranchId = branchId,
            CustomerId = customerId,
            SalesOrderId = salesOrder.Id,
            InvoiceDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            Notes = "Integration sales invoice",
            Lines =
            [
                new SaveSalesInvoiceLineRequest
                {
                    ProductId = line.ProductId,
                    Quantity = 20m,
                    UnitPrice = 75m,
                    TaxPercent = 14m,
                    SalesOrderLineId = line.Id
                }
            ]
        });

        return await response.ReadAsAsync<Guid>();
    }

    private static async Task ApproveRequestAsync(HttpClient client, ApprovalDocumentType documentType, Guid documentId)
    {
        var requestId = await GetApprovalRequestIdAsync(client, documentType, documentId);
        var response = await client.PostAsJsonAsync($"/api/v1/approvals/requests/{requestId}/approve", new
        {
            comments = "Approved by integration test"
        });

        await response.EnsureSuccessWithBodyAsync();
    }

    private static async Task<Guid> GetApprovalRequestIdAsync(HttpClient client, ApprovalDocumentType documentType, Guid documentId)
    {
        var approvals = await GetPagedAsync<ApprovalRequestDto>(
            client,
            $"/api/v1/approvals/requests?pageNumber=1&pageSize=100&documentType={(int)documentType}&status={(int)ApprovalStatus.Pending}");

        return approvals.Items.Single(x => x.DocumentId == documentId).Id;
    }

    private static async Task<PurchaseOrderDto> GetPurchaseOrderAsync(HttpClient client, Guid purchaseOrderId)
    {
        var response = await client.GetAsync($"/api/v1/purchase-orders/{purchaseOrderId}");
        return await response.ReadAsAsync<PurchaseOrderDto>();
    }

    private static async Task<SalesOrderDto> GetSalesOrderAsync(HttpClient client, Guid salesOrderId)
    {
        var response = await client.GetAsync($"/api/v1/sales-orders/{salesOrderId}");
        return await response.ReadAsAsync<SalesOrderDto>();
    }

    private static async Task<InvoiceDetailDto> GetPurchaseInvoiceAsync(HttpClient client, Guid invoiceId)
    {
        var response = await client.GetAsync($"/api/v1/invoices/purchase/{invoiceId}");
        return await response.ReadAsAsync<InvoiceDetailDto>();
    }

    private static async Task<InvoiceDetailDto> GetSalesInvoiceAsync(HttpClient client, Guid invoiceId)
    {
        var response = await client.GetAsync($"/api/v1/invoices/sales/{invoiceId}");
        return await response.ReadAsAsync<InvoiceDetailDto>();
    }

    private static async Task<StockBalanceDto> GetStockBalanceAsync(HttpClient client, Guid branchId, Guid productId)
    {
        var response = await client.GetAsync($"/api/v1/inventory/balances?pageNumber=1&pageSize=50&branchId={branchId}");
        var result = await response.ReadAsAsync<PagedResult<StockBalanceDto>>();
        return result.Items.Single(x => x.ProductId == productId);
    }

    private static async Task<LookupDto> GetLookupByCodeAsync(HttpClient client, string path, string code)
    {
        var response = await client.GetAsync(path);
        var items = await response.ReadAsAsync<List<LookupDto>>();
        return items.Single(x => x.Code == code);
    }

    private static async Task<PagedResult<T>> GetPagedAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        return await response.ReadAsAsync<PagedResult<T>>();
    }
}
