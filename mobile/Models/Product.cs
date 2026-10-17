namespace Solvix.Sims.Models;

public enum StockStatus { Optimal, Low, OutOfStock }

public class Product
{
	public string CompanyId { get; set; } = "solvix-robotics";
	public string Warehouse { get; set; } = "";
	public string Sku { get; init; } = "";
	public string Name { get; init; } = "";
	public string Description { get; init; } = "";
	public string Group { get; init; } = "";          // "Finished Goods" / "Raw Materials"
	public string CategoryLabel { get; init; } = "";  // "Industrial Robotics / Electronics"
	public string Area { get; init; } = "";           // "Electronics • Rack A"
	public string Zone { get; init; } = "";           // "Zone A4"
	public string Bin { get; init; } = "";            // "A4-R12-S02"
	public string BinLong { get; init; } = "";        // "Zone A4 • Rack 12 • Shelf 02"
	public string Barcode { get; init; } = "";
	public string Icon { get; init; } = "inventory_2"; // Material Symbols name, replace with ImageUrl if you have photos
	public string? ImageUrl { get; init; }
	public string Batch { get; init; } = "";

	public int OnHand { get; set; }
	public int Reserved { get; init; }
	public int Quarantined { get; init; }
	public int MinLevel { get; init; }
	public int MaxCapacity { get; init; }
	public decimal UnitCost { get; init; }

	public string Supplier { get; init; } = "";
	public string SupplierNote { get; init; } = "";
	public int LeadTimeDays { get; init; }
	public string LeadMode { get; init; } = "";

	public StockStatus Status =>
		OnHand <= 0 ? StockStatus.OutOfStock :
		OnHand < MinLevel ? StockStatus.Low :
		StockStatus.Optimal;

	public int Available => Math.Max(0, OnHand - Reserved - Quarantined);
	public decimal Valuation => OnHand * UnitCost;
	public int Deficit => Math.Max(0, MinLevel - OnHand);
	public int StockPercent => MinLevel <= 0 ? 100 : (int)Math.Min(100, OnHand * 100.0 / MinLevel);
}
