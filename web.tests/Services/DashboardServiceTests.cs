using Moq;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Tests.Support;
namespace Solvix.Web.Tests.Services;
public sealed class DashboardServiceTests
{
    private static DashboardService Service(List<ProductResponse> products,
        List<SaleResponse>? sales = null, List<StockAdjustmentResponse>? adjustments = null)
    {
        var p = new Mock<IProductRepository>(MockBehavior.Strict);
        var s = new Mock<ISupplierRepository>(MockBehavior.Strict);
        var i = new Mock<IInventoryRepository>(MockBehavior.Strict);
        p.Setup(x => x.GetAllAsync()).ReturnsAsync(products);
        s.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<SupplierResponse> { new() });
        i.Setup(x => x.GetAllSalesAsync()).ReturnsAsync(sales ?? new List<SaleResponse>());
        i.Setup(x => x.GetAllAdjustmentsAsync()).ReturnsAsync(adjustments ?? new List<StockAdjustmentResponse>());
        return new DashboardService(p.Object, s.Object, i.Object, TestData.CurrentUser().Object);
    }
    [Fact]
    public async Task GetAsync_CalculatesTotals_AndSeparatesLowFromOutOfStock()
    {
        var service = Service(new List<ProductResponse>
        {
            new() { Id = "healthy", Stock = 10, MinStock = 2, Price = 12.50m },
            new() { Id = "low", Stock = 2, MinStock = 2, Price = 5m },
            new() { Id = "out", Stock = 0, MinStock = 1, Price = 100m }
        });
        var result = await service.GetAsync();
        Assert.Equal(3, result.TotalProducts);
        Assert.Equal(1, result.TotalSuppliers);
        Assert.Equal(12L, result.TotalUnits);
        Assert.Equal(135m, result.InventoryValue);
        Assert.Equal(1, result.LowStockCount);
        Assert.Equal(1, result.OutOfStockCount);
        Assert.Equal(new[] { "out", "low" }, result.LowStockProducts.Select(x => x.Id).ToArray());
    }
    [Fact]
    public async Task GetAsync_WhenNoProducts_ReturnsZeroInventoryTotals()
    {
        var result = await Service(new List<ProductResponse>()).GetAsync();
        Assert.Equal(0, result.TotalProducts);
        Assert.Equal(0L, result.TotalUnits);
        Assert.Equal(0m, result.InventoryValue);
        Assert.Empty(result.LowStockProducts);
    }
    [Fact]
    public async Task GetAsync_ExcludesUnknownDates_AndReturnsTenLatestMovements()
    {
        var origin = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sales = Enumerable.Range(0, 12).Select(n => new SaleResponse
            { Id = $"sale-{n}", OccurredAt = origin.AddMinutes(n) }).ToList();
        sales.Add(new SaleResponse { Id = "legacy-sale", OccurredAt = null });
        var adjustments = new List<StockAdjustmentResponse>
        {
            new() { Id = "legacy-adjustment", OccurredAt = null },
            new() { Id = "dated-adjustment", OccurredAt = origin }
        };
        var result = await Service(new List<ProductResponse>(), sales, adjustments).GetAsync();
        Assert.Equal(Enumerable.Range(2, 10).Reverse().Select(n => $"sale-{n}").ToArray(),
            result.RecentSales.Select(x => x.Id).ToArray());
        Assert.Equal("dated-adjustment", Assert.Single(result.RecentAdjustments).Id);
    }
}
