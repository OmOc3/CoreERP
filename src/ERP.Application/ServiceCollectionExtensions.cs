using ERP.Application.Approvals;
using ERP.Application.Common.Contracts;
using ERP.Application.Dashboard;
using ERP.Application.Inventory;
using ERP.Application.MasterData;
using ERP.Application.Purchasing;
using ERP.Application.Reports;
using ERP.Application.Sales;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
