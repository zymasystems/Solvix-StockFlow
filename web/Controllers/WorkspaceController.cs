using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solvix.Web.Controllers;
[Authorize(Policy = "CompanyIdentity")]
public sealed class WorkspaceController(Solvix.Web.Connected.CompanyMvcContext company) : Controller
{
    [HttpGet("Workspace"), HttpGet("Receiving"), HttpGet("Receiving/Approvals"), HttpGet("Dispatch"), HttpGet("Dispatch/Approvals"), HttpGet("Transfers"), HttpGet("Warehouses"), HttpGet("Employees"), HttpGet("Audits")]
    public async Task<IActionResult> Index()
    {
        var state = await company.LoadAsync();
        ViewData["CompanyContextId"] = state?.Company.Id;
        ViewData["WarehouseContextId"] = company.WarehouseId;
        ViewData["Section"] = Request.Path.Value?.Trim('/').Split('/')[0].ToLowerInvariant() switch
        {
            "receiving" => "Receiving",
            "dispatch" or "sales" => "Dispatch",
            "transfers" => "Transfers",
            "audits" => "Audits",
            "reports" => "Movements",
            "warehouses" or "employees" or "suppliers" or "users" => "Operations",
            _ => "Inventory"
        };
        return View();
    }
}
