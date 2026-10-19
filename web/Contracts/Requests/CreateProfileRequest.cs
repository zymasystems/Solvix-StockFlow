using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class CreateProfileRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Company { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "You must accept the terms.")]
    public bool AcceptTerms { get; set; }
}