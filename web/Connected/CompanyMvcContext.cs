using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Solvix.Contracts;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;

// Selection cookies carry IDs only. CompanyService verifies membership/warehouse access on every request.
public sealed class CompanyMvcContext(CompanyService companies, IHttpContextAccessor accessor)
{
    public const string CompanyCookie = "solvix.company";
    public const string WarehouseCookie = "solvix.warehouse";
    private Task<Solvix.Contracts.Workspace?>? loading;
    public Solvix.Contracts.Workspace? State { get; private set; }
    public string WarehouseId { get; private set; } = "";
    public string Uid => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new ServiceException(401, "Sign in first.");
    public Task<Solvix.Contracts.Workspace?> LoadAsync() => loading ??= LoadCoreAsync();
    private async Task<Solvix.Contracts.Workspace?> LoadCoreAsync()
    {
        var http = accessor.HttpContext!;
        var memberships = (await companies.CompaniesAsync(Uid)).Where(m => m.Member.Status == "Active" && m.Member.Role is "admin" or "staff").ToList();
        var selected = memberships.FirstOrDefault(m => m.Company.Id == http.Request.Cookies[CompanyCookie]) ?? memberships.FirstOrDefault();
        if (selected is null) return null;
        var all = await companies.LoadAsync(Uid, selected.Company.Id, "");
        WarehouseId = all.Warehouses.FirstOrDefault(w => w.Id == http.Request.Cookies[WarehouseCookie])?.Id
            ?? all.Warehouses.FirstOrDefault(w => w.Id == all.Company.DefaultWarehouseId)?.Id
            ?? all.Warehouses.FirstOrDefault()?.Id ?? "";
        State = WarehouseId.Length == 0 ? all : await companies.LoadAsync(Uid, all.Company.Id, WarehouseId);
        return State;
    }
    public async Task<Solvix.Contracts.Workspace> RequireAsync(bool admin = false)
    {
        var state = await LoadAsync() ?? throw new ServiceException(403, "Choose or join a company first.");
        if (admin && state.Permission.Role != "admin") throw new ServiceException(403, "Company administrator permission is required.");
        return state;
    }
    public string RequireWarehouse() => WarehouseId.Length > 0 ? WarehouseId : throw new ServiceException(400, "Add or select an assigned warehouse first.");
    public Guid FormRequestId()
    {
        var form = accessor.HttpContext!.Request.Form;
        return Guid.TryParse(form["CompanyOperationId"], out var id) && id != Guid.Empty ? id
            : throw new ServiceException(400, "This form has expired. Reload it before submitting.");
    }
    public Task<Result> ExecuteAsync(string command, Operation operation)
    {
        if (State is null) throw new ServiceException(403, "Choose a company first.");
        if (operation.RequestId == Guid.Empty) operation.RequestId = FormRequestId();
        return companies.ExecuteAsync(Uid, State.Company.Id, command, operation);
    }
    public static Guid Step(Guid request, string step) => new(Convert.FromHexString(CompanyService.Key(request + ":" + step))[..16]);
}

public sealed class CompanyPageFilter(CompanyMvcContext company) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var state = await company.LoadAsync();
        if (state is null) { context.Result = new RedirectToActionResult("Index", "Companies", null); return; }
        var request = context.HttpContext.Request;
        if (HttpMethods.IsPost(request.Method))
        {
            var form = await request.ReadFormAsync();
            if (form["CompanyContextId"] != state.Company.Id || form["WarehouseContextId"] != company.WarehouseId)
            {
                context.Result = new BadRequestObjectResult("Your company or warehouse selection changed. Reload this form before submitting."); return;
            }
        }
        if (context.Controller is Controller controller)
        {
            controller.ViewData["CompanyContextId"] = state.Company.Id;
            controller.ViewData["WarehouseContextId"] = company.WarehouseId;
            controller.ViewData["CompanyOperationId"] = HttpMethods.IsPost(request.Method) ? company.FormRequestId() : Guid.NewGuid();
        }
        await next();
    }
}
