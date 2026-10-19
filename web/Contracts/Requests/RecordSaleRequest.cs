using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class RecordSaleRequest
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Sale|Return|Transfer)$")]
    public string Type { get; set; } = "Sale";

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Reuse the same ID when retrying the same operation.
    [Required]
    public Guid? RequestId { get; set; }
}