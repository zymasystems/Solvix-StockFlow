using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.ViewModels.Products;

public class ProductCreateViewModel : ProductFormViewModel
{
    [Range(0, int.MaxValue)]
    [Display(Name = "Opening stock")]
    public int OpeningStock { get; set; }
}