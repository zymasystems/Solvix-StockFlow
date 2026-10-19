namespace Solvix.Web.Contracts.Responses;

public class DashboardResponse
{
    public int TotalProducts { get; set; }

    public int TotalSuppliers { get; set; }

    public long TotalUnits { get; set; }

    public decimal InventoryValue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public List<ProductResponse> LowStockProducts { get; set; } = new();

    public List<SaleResponse> RecentSales { get; set; } = new();

    public List<StockAdjustmentResponse> RecentAdjustments
    { get; set; } = new();
}