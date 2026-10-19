namespace Solvix.Sims.Models;

// Display-only employee information. Passwords belong exclusively to Firebase Authentication.
public class UserAccount
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string WarehouseSite { get; set; } = "";
}
