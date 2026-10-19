using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class AdjustStockRequest : IValidatableObject
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Add|Subtract|Set)$")]
    public string Type { get; set; } = "Add";

    [Required, Range(0, int.MaxValue)]
    public int? Quantity { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public Guid? RequestId { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if ((Type == "Add" || Type == "Subtract")
            && Quantity == 0)
        {
            yield return new ValidationResult(
                "Quantity must be greater than zero for Add or Subtract.",
                new[] { nameof(Quantity) });
        }

        if (RequestId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Request ID must not be empty.",
                new[] { nameof(RequestId) });
        }
    }
}