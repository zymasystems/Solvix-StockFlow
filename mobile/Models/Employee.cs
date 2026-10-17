namespace Solvix.Sims.Models;

public class Employee
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string CompanyId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Staff"; // Admin, Manager, Staff
    public string? AssignedWarehouse { get; set; } // null = can manage all warehouses
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
