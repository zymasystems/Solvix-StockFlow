using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Web.Controllers;
[Authorize(Policy=Policies.WebAdmin)]
[AutoValidateAntiforgeryToken]
public sealed class UsersController : Controller
{
    private readonly IUserService users;
    public UsersController(IUserService users)=>this.users=users;
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)
    {
        var page=await users.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View(new UserListViewModel{Users=page.Items.Select(ViewMapping.User).ToList()});
    }
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var p=await users.GetAsync(id);ViewData["Id"]=p.Uid;
        return View(new UserEditViewModel{FirstName=p.FirstName,LastName=p.LastName,Company=p.Company,Role=p.Role,Status=p.Status});
    }
    [HttpPost]
    public async Task<IActionResult> Edit(string id,UserEditViewModel model)
    {
        if(ModelState.IsValid)try{
            await users.UpdateAsync(id,model);return RedirectToAction(nameof(Index));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        ViewData["Id"]=id;return View(model);
    }
}
