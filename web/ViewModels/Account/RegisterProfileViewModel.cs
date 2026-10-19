using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.ViewModels.Account;

public class RegisterProfileViewModel
{
    [Required]
    public string IdToken { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Company name")]
    public string Company { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "You must accept the terms.")]
    public bool AcceptTerms { get; set; }
}