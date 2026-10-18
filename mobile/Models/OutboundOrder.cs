namespace Solvix.Sims.Models;

public class OutboundOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DispatchId { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string WorkOrder { get; set; } = "";
    public string SourceWarehouse { get; set; } = "";
    public string Destination { get; set; } = "";
    public string DestinationWarehouse { get; set; } = "";
    public string Handler { get; set; } = "";
    public string Reference { get; set; } = "";
    public List<DispatchLine> Lines { get; set; } = new();
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReviewedAt { get; set; }
    public string ReviewNote { get; set; } = "";
    public bool AllVerified => Lines.Count > 0 && Lines.All(line => line.Verified);
    public int TotalUnits => Lines.Sum(line => line.Qty);
}
