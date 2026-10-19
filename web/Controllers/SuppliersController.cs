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
[Authorize(Policy=Policies.WebActiveUser)]
[AutoValidateAntiforgeryToken]
public sealed class SuppliersController : Controller
{
    private readonly ISupplierService suppliers;
    public SuppliersController(ISupplierService suppliers)=>this.suppliers=suppliers;
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await suppliers.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new SupplierListViewModel{Suppliers=page.Items.Select(ViewMapping.Supplier).ToList()});
    }
    [HttpGet,Authorize(Policy=Policies.WebAdmin)]
    public IActionResult Create()=>View(new SupplierFormViewModel());
    [HttpPost,Authorize(Policy=Policies.WebAdmin)]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await suppliers.CreateAsync(new CreateSupplierRequest{Name=model.Name,ContactPerson=model.ContactPerson,
                Email=model.Email,Phone=model.Phone,Status=model.Status,Rating=model.Rating});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        return View(model);
    }
    [HttpGet,Authorize(Policy=Policies.WebAdmin)]
    public async Task<IActionResult> Edit(string id)
    {
        var p=await suppliers.GetAsync(id);ViewData["Id"]=p.Id;
        return View(new SupplierFormViewModel{Name=p.Name,ContactPerson=p.ContactPerson,Email=p.Email,
            Phone=p.Phone,Status=p.Status,Rating=p.Rating});
    }
    [HttpPost,Authorize(Policy=Policies.WebAdmin)]
    public async Task<IActionResult> Edit(string id,SupplierFormViewModel model)
    {
        if(ModelState.IsValid)try{
            await suppliers.UpdateAsync(id,new UpdateSupplierRequest{Name=model.Name,ContactPerson=model.ContactPerson,
                Email=model.Email,Phone=model.Phone,Status=model.Status,Rating=model.Rating});
            return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        ViewData["Id"]=id;return View(model);
    }
}
