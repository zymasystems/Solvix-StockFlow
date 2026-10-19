using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.ViewModels.Dashboard;
namespace Solvix.Web.Controllers;
[Authorize(Policy = Policies.WebActiveUser)]
public sealed class DashboardController : Controller
{
    private readonly IDashboardService dashboard; private readonly ICurrentUserService current; private readonly IReportService reports;
    public DashboardController(IDashboardService dashboard,ICurrentUserService current,IReportService reports)
    {this.dashboard=dashboard;this.current=current;this.reports=reports;}
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile=await current.RequireAsync();var data=await dashboard.GetAsync();
        var inventory=await reports.GetInventoryAsync();
        var model=new DashboardViewModel{
            UserDisplayName=profile.DisplayName,TotalProducts=data.TotalProducts,TotalSuppliers=data.TotalSuppliers,
            TotalUnits=data.TotalUnits,InventoryValue=data.InventoryValue,LowStockCount=data.LowStockCount,
            OutOfStockCount=data.OutOfStockCount,LowStockProducts=data.LowStockProducts.Select(ViewMapping.Product).ToList(),
            RecentSales=data.RecentSales.Select(ViewMapping.Sale).ToList(),RecentAdjustments=data.RecentAdjustments.Select(ViewMapping.Adjustment).ToList(),
            Categories=inventory.Products.GroupBy(x=>x.Category).Select(g=>new CategorySummaryViewModel{
                Category=g.Key,ProductCount=g.Count(),StockUnits=g.Sum(x=>x.Stock)}).ToList()};
        return View(profile.Role=="admin"?"Admin":"Staff",model);
    }
}
