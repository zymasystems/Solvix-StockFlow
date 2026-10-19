using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string UserDisplayName { get; set; } = string.Empty;

    public int TotalProducts { get; set; }

    public int TotalSuppliers { get; set; }

    public long TotalUnits { get; set; }

    public decimal InventoryValue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public List<Product> LowStockProducts { get; set; } = new();

    public List<Sale> RecentSales { get; set; } = new();

    public List<StockAdjustment> RecentAdjustments { get; set; } = new();

    public List<CategorySummaryViewModel> Categories { get; set; } = new();
}