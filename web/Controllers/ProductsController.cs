using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Products;
namespace Solvix.Web.Controllers;
[Authorize(Policy = Policies.WebActiveUser)]
[AutoValidateAntiforgeryToken]
public sealed class ProductsController : Controller
{
    private readonly IProductService products;
    public ProductsController(IProductService products)=>this.products=products;
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await products.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new ProductListViewModel{Products=page.Items.Select(ViewMapping.Product).ToList()});
    }
    [HttpGet]
    public IActionResult Create()=>View(new ProductCreateViewModel{CategoryOptions=FormOptions.Categories()});
    [HttpPost]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        if(ModelState.IsValid)try{
            await products.CreateAsync(new CreateProductRequest{Name=model.Name,Sku=model.Sku,Category=model.Category,
                Price=model.Price,MinStock=model.MinStock,OpeningStock=model.OpeningStock});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        model.CategoryOptions=FormOptions.Categories(model.Category);return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var p=await products.GetAsync(id);ViewData["Id"]=p.Id;
        return View(new ProductFormViewModel{Name=p.Name,Sku=p.Sku,Category=p.Category,Price=p.Price,
            MinStock=checked((int)p.MinStock),CategoryOptions=FormOptions.Categories(p.Category)});
    }
    [HttpPost]
    public async Task<IActionResult> Edit(string id,ProductFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await products.UpdateAsync(id,new UpdateProductRequest{Name=model.Name,Sku=model.Sku,Category=model.Category,Price=model.Price,MinStock=model.MinStock});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        ViewData["Id"]=id;model.CategoryOptions=FormOptions.Categories(model.Category);return View(model);
    }
    [HttpPost]
    [Authorize(Policy=Policies.WebAdmin)]
    public async Task<IActionResult> Delete(string id)
    {
        try{await products.DeleteAsync(id);TempData["Success"]="Product deleted.";}
        catch(ServiceException e)when(e.StatusCode==409){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Index));
    }
}
