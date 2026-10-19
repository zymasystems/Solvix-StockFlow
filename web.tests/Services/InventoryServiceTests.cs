using Moq;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Tests.Support;
namespace Solvix.Web.Tests.Services;
public sealed class InventoryServiceTests
{
    [Fact]
    public async Task RecordAsync_PreservesRequestIdAndVerifiedActor_AndTrimsNotes()
    {
        var actor = TestData.Profile(); var id = Guid.NewGuid();
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var response = new SaleResponse();
        inventory.Setup(x => x.RecordSaleAsync("product-001", "Sale", 2L, "Counter sale", id, actor)).ReturnsAsync(response);
        var service = new SalesService(inventory.Object, TestData.CurrentUser(actor).Object);
        var result = await service.RecordAsync(new RecordSaleRequest
        { ProductId = "product-001", Type = "Sale", Quantity = 2, Notes = "  Counter sale  ", RequestId = id });
        Assert.Same(response, result);
        inventory.Verify(x => x.RecordSaleAsync("product-001", "Sale", 2L, "Counter sale", id, actor), Times.Once);
    }
    [Fact]
    public async Task RecordAsync_WhenProductMissing_RejectsWithoutWriting()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var service = new SalesService(inventory.Object, TestData.CurrentUser().Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.RecordAsync(new RecordSaleRequest
        { ProductId = "", Type = "Sale", Quantity = 2, RequestId = Guid.NewGuid() }));
        Assert.Equal(400, error.StatusCode);
        inventory.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task RecordAsync_WhenRepositoryRejectsInsufficientStock_PropagatesConflict()
    {
        var actor = TestData.Profile(); var id = Guid.NewGuid();
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        inventory.Setup(x => x.RecordSaleAsync("product-001", "Sale", 2L, "", id, actor))
            .ThrowsAsync(new ServiceException(409, "Insufficient stock."));
        var service = new SalesService(inventory.Object, TestData.CurrentUser(actor).Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.RecordAsync(new RecordSaleRequest
        { ProductId = "product-001", Type = "Sale", Quantity = 2, RequestId = id }));
        Assert.Equal(409, error.StatusCode);
    }
    [Fact]
    public async Task AdjustAsync_SetZero_PassesZeroWithoutReplacingRequestId()
    {
        var actor = TestData.Profile(); var id = Guid.NewGuid();
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var response = new StockAdjustmentResponse();
        inventory.Setup(x => x.AdjustStockAsync("product-001", "Set", 0L, "Physical count", id, actor)).ReturnsAsync(response);
        var service = new StockService(inventory.Object, TestData.CurrentUser(actor).Object);
        Assert.Same(response, await service.AdjustAsync(new AdjustStockRequest
        { ProductId = "product-001", Type = "Set", Quantity = 0, Reason = "  Physical count  ", RequestId = id }));
        inventory.Verify(x => x.AdjustStockAsync("product-001", "Set", 0L, "Physical count", id, actor), Times.Once);
    }
    [Fact]
    public async Task AdjustAsync_WhenReasonMissing_RejectsWithoutWriting()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var service = new StockService(inventory.Object, TestData.CurrentUser().Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.AdjustAsync(new AdjustStockRequest
        { ProductId = "product-001", Type = "Add", Quantity = 1, Reason = "", RequestId = Guid.NewGuid() }));
        Assert.Equal(400, error.StatusCode);
        inventory.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task AdjustAsync_WhenNotApproved_DoesNotWrite()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var user = TestData.CurrentUser();
        user.Setup(x => x.RequireAsync(false)).ThrowsAsync(new ServiceException(403, "Approval required."));
        var service = new StockService(inventory.Object, user.Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.AdjustAsync(new AdjustStockRequest
        { ProductId = "product-001", Type = "Add", Quantity = 1, Reason = "Delivery", RequestId = Guid.NewGuid() }));
        Assert.Equal(403, error.StatusCode);
        inventory.VerifyNoOtherCalls();
    }
}
