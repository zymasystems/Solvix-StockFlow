using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Sales;

public class SaleListViewModel
{
    public string? SearchTerm { get; set; }

    public string? Type { get; set; }

    public List<Sale> Sales { get; set; } = new();
}