using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class CreateSupplierRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Active|Inactive)$")]
    public string Status { get; set; } = "Active";

    [Range(0, 5)]
    public double Rating { get; set; }
}