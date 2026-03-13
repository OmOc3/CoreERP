using ERP.Domain.Common;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentAssertions;

namespace ERP.UnitTests;

public sealed class DomainWorkflowTests
{
    [Fact]
    public void PurchaseOrder_Should_Move_To_Completed_After_Full_Receipt()
    {
        var order = new PurchaseOrder("PO-0001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null, null);
        var line = new PurchaseOrderLine(Guid.NewGuid(), 5m, 10m, 0m, 0m, null);
        order.ReplaceLines([line]);

        order.SubmitForApproval();
        order.Approve();
        order.RegisterReceipt(line.Id, 5m);

        order.Status.Should().Be(PurchaseOrderStatus.Completed);
        order.Lines.Single().ReceivedQuantity.Should().Be(5m);
    }

    [Fact]
    public void SalesOrder_Should_Be_PartiallyDelivered_When_Not_All_Quantity_Is_Issued()
    {
        var order = new SalesOrder("SO-0001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null, null);
        var line = new SalesOrderLine(Guid.NewGuid(), 8m, 15m, 0m, 0m, null);
        order.ReplaceLines([line]);

        order.SubmitForApproval();
        order.Approve();
        order.RegisterDelivery(line.Id, 3m);

        order.Status.Should().Be(SalesOrderStatus.PartiallyDelivered);
        order.Lines.Single().DeliveredQuantity.Should().Be(3m);
    }

    [Fact]
    public void StockBalance_Should_Use_WeightedAverageCost_On_Receipt()
    {
        var stock = new StockBalance(Guid.NewGuid(), Guid.NewGuid());

        stock.Receive(10m, 5m);
        stock.Receive(5m, 11m);

        stock.QuantityOnHand.Should().Be(15m);
        stock.AverageCost.Should().Be(7m);
        stock.StockValue.Should().Be(105m);
    }

    [Fact]
    public void StockBalance_Should_Block_Negative_Issue_When_Not_Allowed()
    {
        var stock = new StockBalance(Guid.NewGuid(), Guid.NewGuid());
        stock.Receive(2m, 4m);

        var action = () => stock.Issue(3m, allowNegativeStock: false);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*Insufficient available stock*");
    }

    [Fact]
    public void SalesInvoice_Should_Move_To_Paid_When_Payments_And_Returns_Clear_Balance()
    {
        var invoice = new SalesInvoice("SINV-0001", Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow, DateTime.UtcNow.AddDays(30), null);
        invoice.ReplaceLines([new SalesInvoiceLine(Guid.NewGuid(), 2m, 100m, 0m, null)]);
        invoice.Post();

        invoice.ApplyPayment(150m);
        invoice.ApplyReturn(50m);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.OutstandingAmount.Should().Be(0m);
    }
}
