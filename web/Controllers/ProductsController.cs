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
[Authorize(Policy = "CompanyIdentity")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(Solvix.Web.Connected.CompanyPageFilter))]
public sealed class ProductsController : Controller
{
    private readonly IProductService products;
    private readonly Solvix.Web.Connected.CompanyMvcContext? context;
    private readonly ISupplierService suppliers;
    public ProductsController(IProductService products, ISupplierService suppliers, Solvix.Web.Connected.CompanyMvcContext? context = null){this.products=products;this.suppliers=suppliers;this.context=context;}
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await products.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new ProductListViewModel{Products=page.Items.Select(ViewMapping.Product).ToList()});
    }
    [HttpGet]
    public async Task<IActionResult> Create()=>View(new ProductCreateViewModel{CategoryOptions=FormOptions.Categories(),SupplierOptions=await FormOptions.Suppliers(suppliers),WarehouseId=context?.WarehouseId ?? ""});
    [HttpPost]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        if(ModelState.IsValid)try{
            await products.CreateAsync(new CreateProductRequest{Name=model.Name,Sku=model.Sku,Category=model.Category,
                Price=model.Price,MinStock=model.MinStock,OpeningStock=model.OpeningStock,WarehouseId=model.WarehouseId,SupplierId=model.SupplierId});
            if (context is not null && !string.IsNullOrEmpty(model.WarehouseId))
                Response.Cookies.Append(Solvix.Web.Connected.CompanyMvcContext.WarehouseCookie, model.WarehouseId,
                    new CookieOptions { Secure = true, SameSite = SameSiteMode.Lax, Path = "/", IsEssential = true });
            TempData["Success"] = model.OpeningStock > 0 ? "Product saved. Opening stock is pending. Open Stock approvals to confirm arrival, verify the item and approve it." : "Product saved.";
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 404 or 409){ModelState.AddModelError("",e.Message);}
        model.CategoryOptions=FormOptions.Categories(model.Category);model.SupplierOptions=await FormOptions.Suppliers(suppliers,model.SupplierId);return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var p=await products.GetAsync(id);ViewData["Id"]=p.Id;
        return View(new ProductFormViewModel{SupplierId=p.SupplierId,SupplierOptions=await FormOptions.Suppliers(suppliers,p.SupplierId),Name=p.Name,Sku=p.Sku,Category=p.Category,Price=p.Price,
            MinStock=checked((int)p.MinStock),CategoryOptions=FormOptions.Categories(p.Category)});
    }
    [HttpPost]
    public async Task<IActionResult> Edit(string id,ProductFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await products.UpdateAsync(id,new UpdateProductRequest{Name=model.Name,Sku=model.Sku,Category=model.Category,Price=model.Price,MinStock=model.MinStock,SupplierId=model.SupplierId});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 404 or 409){ModelState.AddModelError("",e.Message);}
        ViewData["Id"]=id;model.CategoryOptions=FormOptions.Categories(model.Category);model.SupplierOptions=await FormOptions.Suppliers(suppliers,model.SupplierId);return View(model);
    }
    [HttpPost]
    [Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Delete(string id)
    {
        try{await products.DeleteAsync(id);TempData["Success"]="Product deleted.";}
        catch(ServiceException e)when(e.StatusCode==409){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Index));
    }
}
