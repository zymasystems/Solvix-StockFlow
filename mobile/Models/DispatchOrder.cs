namespace Solvix.Sims.Models;

public enum ApprovalStatus { Pending, Approved, Rejected }
public enum StagedDispatchStatus { NotStaged, Pending, Approved, Rejected }

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
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string CompanyId { get; set; } = "";
	public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
	public StagedDispatchStatus StagedStatus { get; set; } = StagedDispatchStatus.NotStaged;
	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime? ReviewedAt { get; set; }
	public string ReviewNote { get; set; } = "";
	public DateTime? StagedAt { get; set; }
	public DateTime? StagedReviewedAt { get; set; }
	public string StagedReviewNote { get; set; } = "";
	public string WorkOrder { get; set; } = "";
	public string SourceWarehouse { get; set; } = "";
	public string DestinationWarehouse { get; set; } = "";
	public string Destination { get; set; } = "";
	public string DestinationNote { get; set; } = "";
	public string Handler { get; set; } = "";
	public string HandlerNote { get; set; } = "";
	public string Note { get; set; } = "";
	public List<DispatchLine> Lines { get; set; } = new();
	public bool Completed { get; set; }

	public int TotalUnits => Lines.Sum(l => l.Qty);
	public int VerifiedUnits => Lines.Where(l => l.Verified).Sum(l => l.Qty);
	public int VerifiedLines => Lines.Count(l => l.Verified);
	public int Percent => TotalUnits == 0 ? 0 : (int)Math.Round(VerifiedUnits * 100.0 / TotalUnits);
	public DispatchLine? NextPending => Lines.FirstOrDefault(l => !l.Verified);
	public bool AllVerified => Lines.All(l => l.Verified);
}
