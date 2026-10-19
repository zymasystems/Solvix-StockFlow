using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Stock;
namespace Solvix.Web.Controllers;
[Authorize(Policy="CompanyIdentity")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(Solvix.Web.Connected.CompanyPageFilter))]
public sealed class StockController : Controller
{
    private readonly IStockService stock;private readonly IProductService products;
    public StockController(IStockService stock,IProductService products){this.stock=stock;this.products=products;}
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)=>await History(pageSize,cursor);
    [HttpGet]
    public async Task<IActionResult> History(int pageSize=50,string? cursor=null)
    {
        var page=await stock.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View("History",new StockHistoryViewModel{Adjustments=page.Items.Select(ViewMapping.Adjustment).ToList()});
    }
    [HttpGet, Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Adjust()=>View(new StockAdjustmentCreateViewModel{
        RequestId=Guid.NewGuid(),ProductOptions=await FormOptions.Products(products)});
    [HttpPost, Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Adjust(StockAdjustmentCreateViewModel model)
    {
        if(ModelState.IsValid)try{
            await stock.AdjustAsync(new AdjustStockRequest{ProductId=model.ProductId,Type=model.Type,
                Quantity=model.Quantity,Reason=model.Reason,RequestId=model.RequestId});
            TempData["Success"] = model.Type == "Set" ? "Count submitted for audit approval. Stock has not changed." : "Adjustment posted.";
            return RedirectToAction(nameof(History));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        model.ProductOptions=await FormOptions.Products(products,model.ProductId);return View(model);
    }
}
