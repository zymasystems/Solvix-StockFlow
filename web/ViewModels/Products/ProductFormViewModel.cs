using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Solvix.Web.ViewModels.Products;

public class ProductFormViewModel
{
    [Required, StringLength(150)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Display(Name = "Unit cost")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Minimum stock level")]
    public int MinStock { get; set; }

    // Populated by the controller, not submitted by the user.
    [BindNever]
    public List<SelectListItem> CategoryOptions { get; set; } = new();
}