using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Reports;

public class InventoryReportViewModel
{
    public DateTime GeneratedAtUtc { get; set; }

    public int TotalProducts { get; set; }

    public long TotalUnits { get; set; }

    public decimal TotalInventoryValue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public List<Product> Products { get; set; } = new();

    public List<Sale> Sales { get; set; } = new();

    public List<StockAdjustment> Adjustments { get; set; } = new();
}