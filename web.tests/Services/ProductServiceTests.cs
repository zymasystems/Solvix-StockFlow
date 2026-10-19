using Moq;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Tests.Support;
namespace Solvix.Web.Tests.Services;
public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenPriceHasExtraDecimals_RejectsBeforeWriting()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var user = TestData.CurrentUser();
        var service = new ProductService(products.Object, user.Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.CreateAsync(TestData.Product(12.345m)));
        Assert.Equal(400, error.StatusCode);
        products.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task CreateAsync_WhenNameMissing_RejectsBeforeWriting()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var request = TestData.Product(); request.Name = "";
        var service = new ProductService(products.Object, TestData.CurrentUser().Object);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.CreateAsync(request));
        Assert.Equal(400, error.StatusCode);
        products.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task CreateAsync_WhenValid_PassesVerifiedActorToRepository()
    {
        var actor = TestData.Profile(); var user = TestData.CurrentUser(actor);
        var request = TestData.Product(); var response = new ProductResponse { Id = "product-001" };
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        products.Setup(x => x.CreateAsync(request, actor)).ReturnsAsync(response);
        var service = new ProductService(products.Object, user.Object);
        Assert.Same(response, await service.CreateAsync(request));
        products.Verify(x => x.CreateAsync(request, actor), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_WhenAdminApproved_DeletesOnce()
    {
        var user = TestData.CurrentUser(TestData.Profile("admin"));
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        products.Setup(x => x.DeleteAsync("product-001")).Returns(Task.CompletedTask);
        await new ProductService(products.Object, user.Object).DeleteAsync("product-001");
        user.Verify(x => x.RequireAsync(true), Times.Once);
        products.Verify(x => x.DeleteAsync("product-001"), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_WhenPermissionDenied_DoesNotTouchRepository()
    {
        var user = TestData.CurrentUser();
        user.Setup(x => x.RequireAsync(true)).ThrowsAsync(new ServiceException(403, "Administrator required."));
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var service = new ProductService(products.Object, user.Object);
        Assert.Equal(403, (await Assert.ThrowsAsync<ServiceException>(() => service.DeleteAsync("product-001"))).StatusCode);
        products.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task DeleteAsync_WhenStockConflict_PropagatesConflict()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        products.Setup(x => x.DeleteAsync("product-001")).ThrowsAsync(new ServiceException(409, "Stock must be zero."));
        var service = new ProductService(products.Object, TestData.CurrentUser(TestData.Profile("admin")).Object);
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => service.DeleteAsync("product-001"))).StatusCode);
    }
}
