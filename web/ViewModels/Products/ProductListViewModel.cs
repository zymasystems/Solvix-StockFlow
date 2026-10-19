using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Products;

public class ProductListViewModel
{
    public string? SearchTerm { get; set; }

    public string? Category { get; set; }

    public List<string> Categories { get; set; } = new();

    public List<Product> Products { get; set; } = new();
}