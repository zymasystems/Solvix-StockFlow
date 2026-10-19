using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Contracts;
using Solvix.Web.Connected;

namespace Solvix.Web.ApiControllers;
[ApiController, Authorize(Policy = "CompanyIdentity")]
public sealed class CompaniesApiController(CompanyService companies, IConnectedProfiles profiles) : ControllerBase
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet("api/me/companies")]
    public Task<List<Membership>> Companies() => companies.CompaniesAsync(Uid);
    [HttpPost("api/companies")]
    public Task<Result> Create(Operation input) => companies.CreateAsync(Uid, input);
    [HttpPost("api/companies/{company}/join")]
    public Task<Result> Join(string company, Operation input) => companies.JoinAsync(Uid, company, input);
    [HttpGet("api/companies/{company}/workspace")]
    public Task<Solvix.Contracts.Workspace> Load(string company, string warehouseId = "", string? search = null) => companies.LoadAsync(Uid, company, warehouseId, search);
    [HttpPost("api/companies/{company}/commands/{command}")]
    public Task<Result> Command(string company, string command, Operation input) => companies.ExecuteAsync(Uid, company, command, input);
    [HttpGet("api/companies/{company}/barcode/{code}")]
    public async Task<object> Barcode(string company, string code, string warehouseId = "")
    {
        var state = await companies.LoadAsync(Uid, company, warehouseId, code);
        var matches = state.Products.Where(p => p.Barcode.Equals(code, StringComparison.OrdinalIgnoreCase) || p.Sku.Equals(code, StringComparison.OrdinalIgnoreCase)).ToList();
        return new
        {
            Products = matches,
            Stock = state.Stock.Where(s => matches.Any(p => p.Id == s.ProductId))
        };
    }

    [HttpGet("api/companies/{company}/resources/{resource}")]
    public async Task<ResourcePage> Page(string company, string resource, string warehouseId = "", string? search = null, int pageSize = 20, string? cursor = null)
    {
        if (pageSize is < 1 or > 100)
            throw new Solvix.Web.Services.ServiceException(400, "Page size must be 1–100.");
        var state = await companies.LoadAsync(Uid, company, warehouseId, search);
        IEnumerable<Entity> rows = resource switch
        {
            "products" => state.Products,
            "warehouses" => state.Warehouses,
            "suppliers" => state.Suppliers,
            "members" => state.Members,
            "receipts" => state.Receipts,
            "dispatches" => state.Dispatches,
            "transfers" => state.Transfers,
            "audits" => state.Audits,
            "movements" => state.Movements,
            _ => throw new Solvix.Web.Services.ServiceException(400, "Unsupported resource.")};
        var page = rows.OrderBy(x => x.Id, StringComparer.Ordinal).Where(x => cursor is null || string.CompareOrdinal(x.Id, cursor) > 0).Take(pageSize + 1).ToList();
        return new()
        {
            Items = page.Take(pageSize).Select(x => System.Text.Json.JsonSerializer.SerializeToElement(x, x.GetType(), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))).ToList(),
            NextCursor = page.Count > pageSize ? page[pageSize - 1].Id : null
        };
    }

    [HttpGet("api/me/profile")]
    public Task<Profile> Profile() => profiles.GetAsync(Uid);
    [HttpPut("api/me/profile")]
    public async Task<IActionResult> SaveProfile(Operation input)
    {
        await profiles.SaveAsync(Uid, input);
        return NoContent();
    }
}
