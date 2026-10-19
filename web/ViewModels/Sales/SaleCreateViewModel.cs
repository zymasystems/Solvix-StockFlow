using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Solvix.Web.ViewModels.Sales;
public class SaleCreateViewModel : IValidatableObject
{
    [Required] public string ProductId {get;set;}=string.Empty;
    [Required,RegularExpression("^(Sale|Return|Transfer)$")] public string Type {get;set;}="Sale";
    [Range(1,int.MaxValue)] public int Quantity {get;set;}=1;
    [StringLength(128)] public string? DestinationWarehouseId {get;set;}
    [BindNever] public List<SelectListItem> WarehouseOptions {get;set;}=new();
    [StringLength(1000)] public string? Notes {get;set;}
    [Required] public Guid? RequestId {get;set;}
    [BindNever] public List<SelectListItem> ProductOptions {get;set;}=new();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty) yield return new ValidationResult("Invalid request ID.",new[]{nameof(RequestId)});
    }
}
