using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Sales;
namespace Solvix.Web.Controllers;
[Authorize(Policy=Policies.WebActiveUser)]
[AutoValidateAntiforgeryToken]
public sealed class SalesController : Controller
{
    private readonly ISalesService sales;private readonly IProductService products;
    public SalesController(ISalesService sales,IProductService products){this.sales=sales;this.products=products;}
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await sales.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new SaleListViewModel{Sales=page.Items.Select(ViewMapping.Sale).ToList()});
    }
    [HttpGet]
    public async Task<IActionResult> Create()=>View(new SaleCreateViewModel{
        RequestId=Guid.NewGuid(),ProductOptions=await FormOptions.Products(products)});
    [HttpPost]
    public async Task<IActionResult> Create(SaleCreateViewModel model)
    {
        if(ModelState.IsValid)try{
            await sales.RecordAsync(new RecordSaleRequest{ProductId=model.ProductId,Type=model.Type,
                Quantity=model.Quantity,Notes=model.Notes,RequestId=model.RequestId});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        model.ProductOptions=await FormOptions.Products(products,model.ProductId);
        return View(model); // Preserve RequestId for retry; never generate another here.
    }
}
