namespace Solvix.Web.ViewModels.Dashboard;

public class CategorySummaryViewModel
{
    public string Category { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public long StockUnits { get; set; }
}