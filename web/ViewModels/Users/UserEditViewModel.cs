using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.ViewModels.Users;

public class UserEditViewModel
{
    [Required, RegularExpression("^(admin|staff)$")]
    public string CompanyRole { get; set; } = "staff";

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Company { get; set; }

    [Required]
    [RegularExpression("^(admin|staff)$")]
    public string Role { get; set; } = "staff";

    [Required]
    [RegularExpression("^(Active|Inactive)$")]
    public string Status { get; set; } = "Active";
}