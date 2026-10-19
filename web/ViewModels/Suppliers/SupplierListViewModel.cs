using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Suppliers;

public class SupplierListViewModel
{
    public string? SearchTerm { get; set; }

    public string? Status { get; set; }

    public List<Supplier> Suppliers { get; set; } = new();
}