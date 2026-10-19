using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Stock;

public class StockHistoryViewModel
{
    public string? SearchTerm { get; set; }

    public string? Type { get; set; }

    public List<StockAdjustment> Adjustments { get; set; } = new();
}