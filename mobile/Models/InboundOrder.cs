namespace Solvix.Sims.Models;

public class InboundOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CompanyId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public string ProductWarehouse { get; set; } = "";
    public string SourceWarehouse { get; set; } = "";
    public bool RequiresArrivalConfirmation { get; set; }
    public bool Arrived { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTime? ReviewedAt { get; set; }
    public string ReviewNote { get; set; } = "";
    public bool Verified { get; set; }
    public string VerifiedBarcode { get; set; } = "";
    public DateTime? VerifiedAt { get; set; }
}
