namespace Solvix.Sims.Services.Authentication;

// Mirrors the existing web Contracts/Responses/ProfileResponse; never contains a password.
public sealed class BackendProfile
{
    public string Uid { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public string? PhotoUrl { get; set; }
}

public sealed class RegisterProfileInput
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Company { get; set; } = "";
    public bool AcceptTerms { get; set; }
}
