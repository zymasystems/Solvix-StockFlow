using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.ViewModels.Account;

public class FirebaseLoginViewModel
{
    [Required]
    public string IdToken { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}