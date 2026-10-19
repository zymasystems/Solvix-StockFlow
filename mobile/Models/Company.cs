namespace Solvix.Sims.Models;

public class Company
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Code { get; set; } = ""; // e.g., "SVX", "APX", "BPH", "NDX"
	public string Industry { get; set; } = ""; // e.g., "Robotics & Automation", "Freight & Logistics"
	public string Description { get; set; } = "";
	public string LogoUrl { get; set; } = "";
	public string Phone { get; set; } = "";
	public string Address { get; set; } = "";
	public string Icon { get; set; } = "domain"; // Material Symbols name
	public string ThemeColor { get; set; } = "from-primary-container to-secondary";
	public string AccentClass { get; set; } = "bg-primary text-on-primary";
	public List<string> Warehouses { get; set; } = new();
	public Dictionary<string, WarehouseProfile> WarehouseProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	public string DefaultSite { get; set; } = "";
	public string ContactEmail { get; set; } = "";
	public string EmailDomain { get; set; } = "";
	public string CurrencySymbol { get; set; } = "R";
	public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class WarehouseProfile
{
	public string Type { get; set; } = "Distribution warehouse";
	public string Address { get; set; } = "";
	public string Manager { get; set; } = "";
	public string Phone { get; set; } = "";
	public string Email { get; set; } = "";
	public string Capacity { get; set; } = "";
	public string OperatingHours { get; set; } = "";
	public string Notes { get; set; } = "";
	public List<WarehouseDocument> Documents { get; set; } = new();
}

public class WarehouseDocument
{
	public string FileName { get; set; } = "";
	public string DataUrl { get; set; } = "";
}

public class SupplierContact
{
    public string Id {get;set;} = "";
	public string CompanyId { get; set; } = "";
	public string Name { get; set; } = "";
	public string ContactName { get; set; } = "";
	public string Email { get; set; } = "";
	public string Phone { get; set; } = "";
	public string LeadTime { get; set; } = "";
	public string Notes { get; set; } = "";
}
