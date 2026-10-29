using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.ViewModels.Products;

public class ProductCreateViewModel : ProductFormViewModel
{
    [StringLength(128)]
    public string WarehouseId { get; set; } = "";
    [Range(0, int.MaxValue)]
    [Display(Name = "Opening stock")]
    public int OpeningStock { get; set; }
}