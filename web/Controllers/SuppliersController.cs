using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Suppliers;
namespace Solvix.Web.Controllers;
[Authorize(Policy="CompanyIdentity")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(Solvix.Web.Connected.CompanyPageFilter))]
public sealed class SuppliersController : Controller
{
    private readonly ISupplierService suppliers;
    private readonly Solvix.Web.Connected.CompanyMvcContext context;
    public SuppliersController(ISupplierService suppliers, Solvix.Web.Connected.CompanyMvcContext context){this.suppliers=suppliers;this.context=context;}
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await suppliers.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new SupplierListViewModel{Suppliers=page.Items.Select(ViewMapping.Supplier).ToList()});
    }
    [HttpGet,Authorize(Policy="CompanyAdmin")]
    public IActionResult Create()=>View(new SupplierFormViewModel{WarehouseId=context.WarehouseId});
    [HttpPost,Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await suppliers.CreateAsync(new CreateSupplierRequest{WarehouseId=model.WarehouseId,Name=model.Name,ContactPerson=model.ContactPerson,
                Email=model.Email,Phone=model.Phone,Status=model.Status,Rating=model.Rating});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 404 or 409){ModelState.AddModelError("",e.Message);}
        return View(model);
    }
    [HttpGet,Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Edit(string id)
    {
        var p=await suppliers.GetAsync(id);ViewData["Id"]=p.Id;
        return View(new SupplierFormViewModel{WarehouseId=p.WarehouseId,Name=p.Name,ContactPerson=p.ContactPerson,Email=p.Email,
            Phone=p.Phone,Status=p.Status,Rating=p.Rating});
    }
    [HttpPost,Authorize(Policy="CompanyAdmin")]
    public async Task<IActionResult> Edit(string id,SupplierFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await suppliers.UpdateAsync(id,new UpdateSupplierRequest{WarehouseId=model.WarehouseId,Name=model.Name,ContactPerson=model.ContactPerson,
                Email=model.Email,Phone=model.Phone,Status=model.Status,Rating=model.Rating});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 404 or 409){ModelState.AddModelError("",e.Message);}
        ViewData["Id"]=id;return View(model);
    }
}
