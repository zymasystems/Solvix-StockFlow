namespace Solvix.Sims.Models;

public enum StockStatus { Optimal, Low, OutOfStock }

public class Product
{
    public string ProductId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public long Version { get; set; }
	public string CompanyId { get; set; } = "";
	public string Warehouse { get; set; } = "";
	public string Sku { get; set; } = "";
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public string Group { get; set; } = "";          // "Finished Goods" / "Raw Materials"
	public string CategoryLabel { get; set; } = "";  // "Industrial Robotics / Electronics"
	public string Area { get; set; } = "";           // "Electronics • Rack A"
	public string Zone { get; set; } = "";           // "Zone A4"
	public string Bin { get; set; } = "";            // "A4-R12-S02"
	public string BinLong { get; set; } = "";        // "Zone A4 • Rack 12 • Shelf 02"
	public string Barcode { get; set; } = "";
	public string Icon { get; set; } = "inventory_2"; // Material Symbols name, replace with ImageUrl if you have photos
	public string? ImageUrl { get; set; }
	public string Batch { get; set; } = "";

	public int OnHand { get; set; }
	public int Reserved { get; set; }
	public int Quarantined { get; set; }
	public int MinLevel { get; set; }
	public int MaxCapacity { get; set; }
	public decimal UnitCost { get; set; }

	public string Supplier { get; set; } = "";
	public string SupplierNote { get; set; } = "";
	public int LeadTimeDays { get; set; }
	public string LeadMode { get; set; } = "";

	public StockStatus Status =>
		OnHand <= 0 ? StockStatus.OutOfStock :
		OnHand <= MinLevel ? StockStatus.Low :
		StockStatus.Optimal;

	public int Available => Math.Max(0, OnHand - Reserved - Quarantined);
	public decimal Valuation => OnHand * UnitCost;
	public int Deficit => Math.Max(0, MinLevel - OnHand);
	public int StockPercent => MinLevel <= 0 ? 100 : (int)Math.Min(100, OnHand * 100.0 / MinLevel);
}
