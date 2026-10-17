namespace Solvix.Sims.Models;

public class DispatchLine
{
	public string Sku { get; set; } = "";
	public string Name { get; set; } = "";
	public string Bin { get; set; } = "";
	public string Icon { get; set; } = "inventory_2";
	public int Qty { get; set; }
	public bool Verified { get; set; }
}

public class DispatchOrder
{
	public string CompanyId { get; set; } = "solvix-robotics";
	public string WorkOrder { get; set; } = "WO-9042";
	public string Destination { get; set; } = "Robotics Bay #2";
	public string DestinationNote { get; set; } = "Line Transfer";
	public string Handler { get; set; } = "Station Terminal 04";
	public string HandlerNote { get; set; } = "Internal Logistics";
	public string Note { get; set; } = "Priority feed for Assembly Shift B robotics calibration cycle.";
	public List<DispatchLine> Lines { get; set; } = new();
	public bool Completed { get; set; }

	public int TotalUnits => Lines.Sum(l => l.Qty);
	public int VerifiedUnits => Lines.Where(l => l.Verified).Sum(l => l.Qty);
	public int VerifiedLines => Lines.Count(l => l.Verified);
	public int Percent => TotalUnits == 0 ? 0 : (int)Math.Round(VerifiedUnits * 100.0 / TotalUnits);
	public DispatchLine? NextPending => Lines.FirstOrDefault(l => !l.Verified);
	public bool AllVerified => Lines.All(l => l.Verified);
}
