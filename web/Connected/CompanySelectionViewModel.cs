using Solvix.Contracts;
namespace Solvix.Web.Connected;
public sealed class CompanySelectionViewModel
{
    public List<Membership> Memberships { get; set; } = new();
    public Solvix.Contracts.Workspace? State { get; set; }
    public string WarehouseId { get; set; } = "";
}
