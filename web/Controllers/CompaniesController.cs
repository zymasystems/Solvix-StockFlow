using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Services;
using Solvix.Web.Authorization;
namespace Solvix.Web.Controllers;
[Authorize(Policy = "CompanyIdentity"), AutoValidateAntiforgeryToken]
public sealed class CompaniesController(CompanyService companies, CompanyMvcContext context, CompanyDeletionService deletion) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var state = await context.LoadAsync();
        if (HttpContext.Items["Solvix.CompanyFallback"] is string selectedName && !TempData.ContainsKey("Success"))
            TempData["Success"] = "Active company: " + selectedName + ". Open another company below to change it.";
        return View(new CompanySelectionViewModel { Memberships = await companies.CompaniesAsync(context.Uid), Panels = await companies.ManagementPanelsAsync(context.Uid), PendingDeletions = await companies.IsPlatformAdministratorAsync(context.Uid) ? await deletion.PendingAsync(context.Uid) : new(), State = state, WarehouseId = context.WarehouseId });
    }
    [HttpPost]
    public async Task<IActionResult> Select(string companyId, string? warehouseId)
    {
        await companies.EnsurePlatformAdministratorAccessAsync(context.Uid, companyId);
        var state = await companies.LoadAsync(context.Uid, companyId, warehouseId ?? "");
        var warehouse = string.IsNullOrEmpty(warehouseId) ? state.Warehouses.FirstOrDefault()?.Id ?? "" : warehouseId;
        SelectContext(state.Company.Id, warehouse);
        return RedirectToAction("Index", "Dashboard");
    }
    [HttpPost]
    public async Task<IActionResult> Create(string name, Guid requestId)
    {
        try
        {
            var result = await companies.CreateAsync(context.Uid, new() { Name = name ?? "", RequestId = requestId });
            TempData["Success"] = result.Message;
            if (result.MembershipStatus != "Active") return RedirectToAction(nameof(Index));
            var state = await companies.LoadAsync(context.Uid, result.Id, "");
            SelectContext(result.Id, state.Company.DefaultWarehouseId);
            return RedirectToAction("Index", "Dashboard");
        }
        catch (ServiceException e) when (e.StatusCode is 400 or 409 or 410) { ModelState.AddModelError("", e.Message); return await Index(); }
    }
    [HttpPost]
    public async Task<IActionResult> Join(string companyId, string fullName)
    {
        try { var result = await companies.JoinAsync(context.Uid, companyId ?? "", new() { Name = fullName ?? "" }); TempData["Success"] = result.Message; }
        catch (ServiceException e) when (e.StatusCode is 400 or 404 or 409) { ModelState.AddModelError("", e.Message); }
        return await Index();
    }
    [HttpPost]
    public async Task<IActionResult> AddWarehouse(string companyId, string name, Guid requestId)
    {
        try
        {
            await companies.EnsurePlatformAdministratorAccessAsync(context.Uid, companyId);
            var result = await companies.ExecuteAsync(context.Uid, companyId, "warehouse.save",
                new() { Name = name ?? "", RequestId = requestId });
            SelectContext(companyId, result.Id);
            TempData["Success"] = "Warehouse created and selected. You can now add products.";
            return RedirectToAction("Create", "Products");
        }
        catch (ServiceException e) when (e.StatusCode is 400 or 403 or 404 or 409 or 410)
        {
            ModelState.AddModelError("", e.Message);
            return await Index();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Rename(string companyId, string name, Guid requestId)
    {
        try
        {
            await companies.EnsurePlatformAdministratorAccessAsync(context.Uid, companyId);
            await companies.RequireAdministratorAsync(context.Uid, companyId);
            var state = await companies.LoadAsync(context.Uid, companyId, "");
            await companies.ExecuteAsync(context.Uid, companyId, "company.brand", new()
            {
                Name = name ?? "", RequestId = requestId,
                PhotoUrl = state.Company.LogoUrl, Color = state.Company.Color
            });
            TempData["Success"] = "Company name updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ServiceException e) when (e.StatusCode is 400 or 403 or 404 or 409 or 410)
        {
            ModelState.AddModelError("", e.Message);
            return await Index();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReviewMember(string companyId, string uid, string role, string status, List<string>? warehouseIds, Guid requestId)
    {
        try
        {
            await companies.EnsurePlatformAdministratorAccessAsync(context.Uid, companyId);
            await companies.ExecuteAsync(context.Uid, companyId, "member.review", new()
            {
                Id = uid, Role = role, Status = status, WarehouseIds = warehouseIds ?? new(), RequestId = requestId
            });
            TempData["Success"] = "Company role, status and warehouse assignments saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (ServiceException e) when (e.StatusCode is 400 or 403 or 404 or 409 or 410)
        {
            ModelState.AddModelError("", e.Message);
            return await Index();
        }
    }

    [HttpGet, Authorize(Policy = Policies.WebAdmin)]
    public async Task<IActionResult> Delete(string id) => View(await deletion.PreviewAsync(context.Uid, id));

    [HttpPost, ActionName("Delete"), Authorize(Policy = Policies.WebAdmin)]
    public async Task<IActionResult> DeleteConfirmed(string id, string confirmationName, bool deleteExclusiveAccounts)
    {
        try
        {
            await deletion.DeleteAsync(context.Uid, id, confirmationName, deleteExclusiveAccounts);
            if (Request.Cookies[CompanyMvcContext.CompanyCookie] == id)
            {
                Response.Cookies.Delete(CompanyMvcContext.CompanyCookie, new CookieOptions { Path = "/" });
                Response.Cookies.Delete(CompanyMvcContext.WarehouseCookie, new CookieOptions { Path = "/" });
            }
            TempData["Success"] = "Company, warehouse data and employee memberships deleted. Shared and system administrator accounts were preserved.";
            return RedirectToAction(nameof(Index));
        }
        catch (ServiceException error) when (error.StatusCode is 400 or 409 or 503)
        {
            ModelState.AddModelError("", error.Message);
            return View("Delete", await deletion.PreviewAsync(context.Uid, id));
        }
    }

    private void SelectContext(string company, string warehouse)
    {
        // Non-secret UI selection only; all access is checked again by CompanyService.
        var options = new CookieOptions { Secure = true, SameSite = SameSiteMode.Lax, Path = "/", IsEssential = true };
        Response.Cookies.Append(CompanyMvcContext.CompanyCookie, company, options);
        Response.Cookies.Append(CompanyMvcContext.WarehouseCookie, warehouse, options);
    }
}
