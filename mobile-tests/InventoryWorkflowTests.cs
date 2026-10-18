using Solvix.Sims.Models;
using Solvix.Sims.Services;
using Xunit;

namespace Solvix.Sims.Tests;

public class InventoryWorkflowTests
{
    private const string Source = "Pretoria Robotics Hub WH-01";
    private const string Destination = "Johannesburg Assembly Bay 4";

    private static (InventoryService Service, SessionState Session) CreateService()
    {
        var session = new SessionState();
        return (new InventoryService(session), session);
    }

    private static Product AddProduct(InventoryService service, string sku = "TEST-001", int quantity = 20) 
    {
        var product = new Product
        {
            Sku = sku,
            Name = "Workflow test item",
            Barcode = "BAR-" + sku,
            Warehouse = Source,
            OnHand = quantity,
            MinLevel = 1,
            UnitCost = 2.50m
        };
        service.AddProduct(product);
        return product;
    }

    [Fact]
    public void WarehouseSelectionScopesInventoryAndReceiptCreatesSeparateWarehouseStock()
    {
        var (service, session) = CreateService();
        var sourceProduct = AddProduct(service);

        Assert.True(service.SelectWarehouse(Destination));
        Assert.Equal(Destination, session.HubName);
        Assert.DoesNotContain(service.WarehouseProducts, p => p.Sku == sourceProduct.Sku);

        Assert.True(service.ReceiveStock(sourceProduct.Sku, 7, Destination, "PO-7", null, Source));
        Assert.Equal(20, sourceProduct.OnHand);
        Assert.Equal(7, service.Find(sourceProduct.Sku, Destination)?.OnHand);
        Assert.Equal(Destination, service.Movements.Last().Warehouse);
    }

    [Fact]
    public void InboundStockIsOnlyAddedAfterArrivalVerificationAndApproval()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service, quantity: 5);
        service.CreateInboundRequest(new InboundOrder
        {
            Sku = product.Sku,
            ProductName = product.Name,
            ProductWarehouse = Source,
            Warehouse = Source,
            Quantity = 4,
            RequiresArrivalConfirmation = true
        });
        var request = Assert.Single(service.InboundRequests);

        Assert.False(service.ReviewInbound(request.Id, approve: true));
        Assert.Equal(5, product.OnHand);
        Assert.True(service.ConfirmInboundArrival(request.Id));
        Assert.False(service.VerifyInbound(request.Id, "wrong-barcode"));
        Assert.True(service.VerifyInbound(request.Id, product.Barcode));
        Assert.True(service.ReviewInbound(request.Id, approve: true));

        Assert.Equal(9, product.OnHand);
        Assert.Equal(ApprovalStatus.Approved, request.ApprovalStatus);
        Assert.Equal(MovementKind.Inbound, service.Movements.Last().Kind);
    }

    [Fact]
    public void RejectedInboundDoesNotChangeStock()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service, quantity: 5);
        service.CreateInboundRequest(new InboundOrder
        {
            Sku = product.Sku,
            ProductName = product.Name,
            ProductWarehouse = Source,
            Warehouse = Source,
            Quantity = 4
        });
        var request = Assert.Single(service.InboundRequests);

        Assert.True(service.ReviewInbound(request.Id, approve: false, note: "Damaged"));

        Assert.Equal(5, product.OnHand);
        Assert.Equal(ApprovalStatus.Rejected, request.ApprovalStatus);
        Assert.DoesNotContain(service.Movements, movement => movement.Sku == product.Sku && movement.Kind == MovementKind.Inbound);
    }

    [Fact]
    public void DispatchMustBeScannedAndStagedBeforeItCanCreateOutbound()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service, quantity: 12);
        var dispatch = new DispatchOrder
        {
            WorkOrder = "WO-42",
            SourceWarehouse = Source,
            Destination = "Customer site",
            Lines = new() { new DispatchLine { Sku = product.Sku, Name = product.Name, Qty = 3 } }
        };
        service.CreateDispatch(dispatch);

        Assert.False(service.ReviewDispatch(dispatch.Id, approve: true));
        Assert.False(service.VerifyDispatchItem("incorrect", product.Sku));
        Assert.True(service.VerifyDispatchItem(product.Barcode, product.Sku));
        Assert.Equal(StagedDispatchStatus.Pending, dispatch.StagedStatus);
        Assert.True(service.ReviewDispatch(dispatch.Id, approve: true));

        var outbound = Assert.Single(service.OutboundRequests);
        Assert.Equal(ApprovalStatus.Pending, outbound.ApprovalStatus);
        Assert.Equal(12, product.OnHand);
        Assert.False(service.ReviewOutbound(outbound.Id, approve: true));
        Assert.True(service.VerifyOutboundItem(outbound.Id, product.Barcode, product.Sku));
        Assert.True(service.ReviewOutbound(outbound.Id, approve: true));

        Assert.Equal(9, product.OnHand);
        Assert.True(dispatch.Completed);
        Assert.Equal(ApprovalStatus.Approved, outbound.ApprovalStatus);
        Assert.Equal(-3, service.Movements.Last().Delta);
    }

    [Fact]
    public void WarehouseTransferCompletesOutboundThenArrivalBeforeDestinationStockIsAdded()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service, quantity: 12);
        var dispatch = new DispatchOrder
        {
            WorkOrder = "TRANSFER-7",
            SourceWarehouse = Source,
            Destination = Destination,
            DestinationWarehouse = Destination,
            Lines = new() { new DispatchLine { Sku = product.Sku, Name = product.Name, Qty = 3 } }
        };
        service.CreateDispatch(dispatch);
        Assert.True(service.VerifyDispatchItem(product.Barcode, product.Sku));
        Assert.True(service.ReviewDispatch(dispatch.Id, approve: true));
        var outbound = Assert.Single(service.OutboundRequests);
        Assert.True(service.VerifyOutboundItem(outbound.Id, product.Barcode, product.Sku));
        Assert.True(service.ReviewOutbound(outbound.Id, approve: true));

        var inbound = Assert.Single(service.InboundRequests);
        Assert.Equal(9, product.OnHand);
        Assert.Null(service.Find(product.Sku, Destination));
        Assert.True(service.SelectWarehouse(Destination));
        Assert.True(service.ConfirmInboundArrival(inbound.Id));
        Assert.True(service.VerifyInbound(inbound.Id, product.Barcode));
        Assert.True(service.ReviewInbound(inbound.Id, approve: true));

        Assert.Equal(9, product.OnHand);
        Assert.Equal(3, service.Find(product.Sku, Destination)?.OnHand);
        Assert.Equal(ApprovalStatus.Approved, inbound.ApprovalStatus);
    }

    [Fact]
    public void AuthenticationRejectsUnknownDomainAndSignsInKnownCompanyUser()
    {
        var (service, session) = CreateService();

        Assert.False(service.TryAuthenticate("person@unknown.test", "secret", session, out var error));
        Assert.Contains("No registered company", error);
        Assert.True(service.TryAuthenticate("ops@apex-freight.com", "password", session, out error));
        Assert.Null(error);
        Assert.Equal("apex-logistics", service.CurrentCompanyId);
        Assert.Equal("Durban Harbor Terminal 03", service.CurrentWarehouse);
        Assert.Equal("Durban Harbor Terminal 03", session.HubName);
    }

    [Fact]
    public void DeletingStockRemovesWarehouseItemAndRecordsRemoval()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service, quantity: 6);

        Assert.True(service.DeleteStockItem(product.Sku, Source, out var error));

        Assert.Empty(error);
        Assert.Null(service.Find(product.Sku, Source));
        Assert.Equal(-6, service.Movements.Last().Delta);
    }

    [Fact]
    public void CompanySwitchKeepsTenantInventoryIsolated()
    {
        var (service, _) = CreateService();
        var product = AddProduct(service);

        service.SelectCompany("apex-logistics");

        Assert.DoesNotContain(service.Products, item => item.Sku == product.Sku);
        Assert.Null(service.Find(product.Sku, Source));
        Assert.Equal("apex-logistics", service.CurrentCompanyId);
    }
}
