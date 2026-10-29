using Solvix.Contracts;
namespace Solvix.Web.Connected;
public sealed class CompanySelectionViewModel
{
    public List<CompanyDeletionJob> PendingDeletions { get; set; } = new();
    public List<CompanyPanel> Panels { get; set; } = new();
    public List<Membership> Memberships { get; set; } = new();
    public Solvix.Contracts.Workspace? State { get; set; }
    public string WarehouseId { get; set; } = "";
}
