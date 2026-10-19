namespace Solvix.Sims.Models;

public class UserAccount
{
	public string Email { get; set; } = "";
	public string Password { get; set; } = "password";
	public string FullName { get; set; } = "";
	public string Role { get; set; } = "Inventory Operations Lead";
	public string CompanyId { get; set; } = "";
	public string WarehouseSite { get; set; } = "";
}
