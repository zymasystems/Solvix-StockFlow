using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solvix.Web.Controllers;
[Authorize(Policy = "CompanyIdentity")]
public sealed class WorkspaceController : Controller
{
    [HttpGet("Workspace"), HttpGet("Dashboard"), HttpGet("Dashboard/Index"), HttpGet("Products"), HttpGet("Suppliers"), HttpGet("Stock"), HttpGet("Reports"), HttpGet("Users"), HttpGet("Sales"), HttpGet("Receiving"), HttpGet("Receiving/Approvals"), HttpGet("Dispatch"), HttpGet("Dispatch/Approvals"), HttpGet("Transfers"), HttpGet("Warehouses"), HttpGet("Employees"), HttpGet("Audits")]
    public IActionResult Index()
    {
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
