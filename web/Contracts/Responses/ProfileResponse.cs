namespace Solvix.Web.Contracts.Responses;

public class ProfileResponse
{
    public string Code { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }
}