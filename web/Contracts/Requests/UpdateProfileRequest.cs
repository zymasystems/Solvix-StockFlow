using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;
}