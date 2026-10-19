using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.ViewModels.Reports;
namespace Solvix.Web.Controllers;
[Authorize(Policy="CompanyIdentity")]
[ServiceFilter(typeof(Solvix.Web.Connected.CompanyPageFilter))]
public sealed class ReportsController : Controller
{
    private readonly IReportService reports;
    public ReportsController(IReportService reports)=>this.reports=reports;
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var r=await reports.GetInventoryAsync();
        return View(new InventoryReportViewModel{GeneratedAtUtc=r.GeneratedAt.UtcDateTime,
            TotalProducts=r.TotalProducts,TotalUnits=r.TotalUnits,TotalInventoryValue=r.TotalInventoryValue,
            LowStockCount=r.LowStockCount,OutOfStockCount=r.OutOfStockCount,Products=r.Products.Select(ViewMapping.Product).ToList()});
    }
}
