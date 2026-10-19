using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Solvix.Web.ViewModels.Stock;
public class StockAdjustmentCreateViewModel : IValidatableObject
{
    [Required] public string ProductId {get;set;}=string.Empty;
    [Required,RegularExpression("^(Add|Subtract|Set)$")] public string Type {get;set;}="Add";
    // Nullable distinguishes a missing input from intentionally setting stock to zero.
    [Required,Range(0,int.MaxValue)] public int? Quantity {get;set;}
    [Required,StringLength(500)] public string Reason {get;set;}=string.Empty;
    [Required] public Guid? RequestId {get;set;}
    [BindNever] public List<SelectListItem> ProductOptions {get;set;}=new();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if((Type=="Add"||Type=="Subtract")&&Quantity==0)
            yield return new ValidationResult("Quantity must be greater than zero.",new[]{nameof(Quantity)});
        if(RequestId==Guid.Empty) yield return new ValidationResult("Invalid request ID.",new[]{nameof(RequestId)});
    }
}
