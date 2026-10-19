using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solvix.Contracts;
using Solvix.Web.Connected;

namespace Solvix.Connected.Tests;
public sealed class HttpIntegrationTests
{
    private sealed class IdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e) : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Uid", out var uid))
                return Task.FromResult(AuthenticateResult.NoResult());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()), new Claim("session_expires_at", DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString()) }, "Test")), "Test")));
        }
    }

    private sealed class Profiles : IConnectedProfiles
    {
        public Task<Profile> GetAsync(string uid) => Task.FromResult(new Profile { Uid = uid, FullName = "Test" });
        public Task SaveAsync(string uid, Operation input) => Task.CompletedTask;
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder b)
        {
            b.UseEnvironment("Testing");
            b.ConfigureServices(s =>
            {
                foreach (var descriptor in s.Where(d => d.ServiceType == typeof(IAuthorizationHandler) && d.ImplementationType?.Name == "ActiveProfileHandler").ToList())
                    s.Remove(descriptor);
                s.RemoveAll<ICompanyStore>();
                s.AddSingleton<ICompanyStore, MemoryStore>();
                s.RemoveAll<IConnectedProfiles>();
                s.AddSingleton<IConnectedProfiles, Profiles>();
                // Production API uses Firebase ID-token verification. Only this in-process test host uses a header identity.
                s.PostConfigure<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = "Test";
                    o.DefaultChallengeScheme = "Test";
                    o.DefaultForbidScheme = "Test";
                });
                s.PostConfigure<AuthorizationOptions>(o => o.FallbackPolicy = new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().Build());
                s.AddAuthentication().AddScheme<AuthenticationSchemeOptions, IdentityHandler>("Test", _ =>
                {
                });
                s.AddAuthorization(o => {
                    o.AddPolicy("CompanyIdentity", new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().Build());
                    o.AddPolicy("CompanyAdmin", new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().AddRequirements(new Solvix.Web.Authorization.CompanyAdminRequirement()).Build());
                });
            });
        }
    }

    [Fact]
    public async Task TwoIndependentClientsSeeSameStockAndOtherCompanyCannotAccess()
    {
        await using var f = new Factory();
        using var web = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var mobile = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        web.DefaultRequestHeaders.Add("X-Test-Uid", "owner");
        mobile.DefaultRequestHeaders.Add("X-Test-Uid", "owner");
        var created = await web.PostAsJsonAsync("/api/companies", new Operation { RequestId = Guid.NewGuid(), Name = "Connected" });
        created.EnsureSuccessStatusCode();
        var cid = (await created.Content.ReadFromJsonAsync<Result>())!.Id;
        async Task Post(string command, Operation op)
        {
            op.RequestId = Guid.NewGuid();
            var response = await web.PostAsJsonAsync($"/api/companies/{cid}/commands/{command}", op);
            response.EnsureSuccessStatusCode();
        }

        await Post("warehouse.save", new() { Id = "wh", Name = "Main" });
        await Post("product.save", new() { Id = "p", Sku = "ABC", Name = "Motor" });
        await Post("stock.adjust", new() { WarehouseId = "wh", ProductId = "p", Quantity = 7, Notes = "Opening" });
        var state = await mobile.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace?warehouseId=wh");
        Assert.Equal(7, state!.Stock.Single().OnHand);
        await Post("product.save", new() { Id = "p2", Sku = "XYZ", Name = "Second", Barcode = "9988" });
        var first = await mobile.GetFromJsonAsync<ResourcePage>($"/api/companies/{cid}/resources/products?pageSize=1");
        Assert.Single(first!.Items);
        Assert.NotNull(first.NextCursor);
        var second = await mobile.GetFromJsonAsync<ResourcePage>($"/api/companies/{cid}/resources/products?pageSize=1&cursor={first.NextCursor}");
        Assert.Single(second!.Items);
        Assert.Null(second.NextCursor);
        Assert.NotEqual(first.Items[0].GetProperty("id").GetString(), second.Items[0].GetProperty("id").GetString());
        var barcode = await mobile.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/companies/{cid}/barcode/9988");
        Assert.Equal("p2", barcode.GetProperty("products")[0].GetProperty("id").GetString());
        using var stranger = f.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        stranger.DefaultRequestHeaders.Add("X-Test-Uid", "other");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/companies/{cid}/workspace")).StatusCode);
        using var anonymous = f.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/me/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await web.GetAsync("/api/products")).StatusCode);
    }
    private static HttpClient Browser(Factory factory, string uid)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }
    private static async Task<string> Seed(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/companies", new Operation { RequestId = Guid.NewGuid(), Name = "Original company", Reference = "Owner Admin" });
        response.EnsureSuccessStatusCode();
        var id = (await response.Content.ReadFromJsonAsync<Result>())!.Id;
        foreach (var (command, op) in new (string, Operation)[] {
            ("warehouse.save", new() { Id = "wh", Name = "Main warehouse" }),
            ("warehouse.save", new() { Id = "dest", Name = "Destination" }),
            ("product.save", new() { Id = "p", Sku = "ABC", Name = "Motor", Category = "Electronics", UnitCost = 10 }),
            ("supplier.save", new() { Id = "supplier", Name = "Original supplier", Email = "s@example.test" }),
            ("stock.adjust", new() { WarehouseId = "wh", ProductId = "p", Quantity = 7, Notes = "Seed" }) })
        {
            op.RequestId = Guid.NewGuid();
            (await client.PostAsJsonAsync($"/api/companies/{id}/commands/{command}", op)).EnsureSuccessStatusCode();
        }
        return id;
    }
    private static async Task<Dictionary<string,string>> Fields(HttpClient client, string route)
    {
        var response = await client.GetAsync(route);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var values = new Dictionary<string,string>();
        foreach (System.Text.RegularExpressions.Match input in System.Text.RegularExpressions.Regex.Matches(html, "<input[^>]+>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            var name = System.Text.RegularExpressions.Regex.Match(input.Value, "name=\"([^\"]+)\"");
            var value = System.Text.RegularExpressions.Regex.Match(input.Value, "value=\"([^\"]*)\"");
            if (name.Success && value.Success) values[WebUtility.HtmlDecode(name.Groups[1].Value)] = WebUtility.HtmlDecode(value.Groups[1].Value);
        }
        Assert.Contains("__RequestVerificationToken", values.Keys);
        return values;
    }

    [Theory]
    [InlineData("/Dashboard", "Admin overview")]
    [InlineData("/Dashboard/Index", "Admin overview")]
    [InlineData("/Products", "Motor")]
    [InlineData("/Products/Create", "Add a product")]
    [InlineData("/Products/Edit/p", "Electronics")]
    [InlineData("/Suppliers", "Original supplier")]
    [InlineData("/Suppliers/Create", "Add a supplier")]
    [InlineData("/Suppliers/Edit/supplier", "Original supplier")]
    [InlineData("/Sales", "Sales &amp; stock out")]
    [InlineData("/Sales/Create", "Record a stock movement")]
    [InlineData("/Stock", "Adjustment history")]
    [InlineData("/Stock/History", "Adjustment history")]
    [InlineData("/Stock/Adjust", "Adjust stock")]
    [InlineData("/Reports", "Inventory report")]
    [InlineData("/Users", "Owner Admin")]
    [InlineData("/Users/Edit/owner", "Owner")]
    public async Task OriginalMvcPagesRenderTheirContentWithoutBlazor(string route, string expected)
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        await Seed(client);
        var response = await client.GetAsync(route);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{route}: {response.StatusCode} {html}");
        Assert.Contains(expected, html);
        Assert.Contains("class=\"sidebar\"", html);
        Assert.Contains("dashboard.css", html);
        Assert.DoesNotContain("blazor.server.js", html);
    }

    [Fact]
    public async Task CompanyOnboardingRendersInsteadOfAnEmptyWorkspace()
    {
        await using var f = new Factory(); using var client = Browser(f, "new-user");
        var dashboard = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
        Assert.Equal("/Companies", dashboard.Headers.Location!.ToString());
        var html = await client.GetStringAsync("/Companies");
        Assert.Contains("Create a company", html); Assert.Contains("Join an existing company", html);
    }

    [Fact]
    public async Task OriginalProductFormCreatesSharedProductAndPendingReceiptWithSafeReplay()
    {
        await using var f = new Factory(); using var web = Browser(f, "owner");
        var cid = await Seed(web); var form = await Fields(web, "/Products/Create");
        form["Name"] = "New product"; form["Sku"] = "NEW"; form["Category"] = "Electronics"; form["Price"] = "12.50"; form["MinStock"] = "2"; form["OpeningStock"] = "3";
        Assert.Equal(HttpStatusCode.Redirect, (await web.PostAsync("/Products/Create", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await web.PostAsync("/Products/Create", new FormUrlEncodedContent(form))).StatusCode);
        using var mobile = Browser(f, "owner");
        var state = (await mobile.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        var product = Assert.Single(state.Products, p => p.Sku == "NEW");
        Assert.Equal(0, Assert.Single(state.Stock, s => s.ProductId == product.Id).OnHand);
        var receipt = Assert.Single(state.Receipts); Assert.Equal("Pending", receipt.Status); Assert.Equal(3, receipt.Quantity);
    }

    [Fact]
    public async Task SaleFormCreatesTransferWithoutReducingStock()
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        var cid = await Seed(client); var form = await Fields(client, "/Sales/Create");
        form["ProductId"] = "p"; form["Type"] = "Transfer"; form["Quantity"] = "2"; form["DestinationWarehouseId"] = "dest";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Sales/Create", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await client.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(7, state.Stock.Sum(s => s.OnHand));
        var dispatch = Assert.Single(state.Dispatches); Assert.Equal("Draft", dispatch.Status); Assert.Equal("dest", dispatch.DestinationWarehouseId);
    }

    [Fact]
    public async Task ChangedCompanyContextRejectsOldFormAndCompanyMembershipDoesNotGrantGlobalAccess()
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        var cid = await Seed(client); var form = await Fields(client, "/Stock/Adjust");
        form["ProductId"] = "p"; form["Type"] = "Subtract"; form["Quantity"] = "1"; form["Reason"] = "Correction";
        form["CompanyContextId"] = "another-company";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await client.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(7, state.Stock.Sum(s => s.OnHand));
        using var stranger = Browser(f, "stranger");
        Assert.Equal(HttpStatusCode.Redirect, (await stranger.GetAsync("/Products/Edit/p")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/products")).StatusCode);
    }

    [Fact]
    public async Task AddedWarehousePagesArePrerenderedWithContent()
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        await Seed(client);
        var response = await client.GetAsync("/Receiving");
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, html);
        Assert.Contains("Submit delivery", html);
        Assert.Contains("Inbound arrivals and approvals", html);
        Assert.Contains("dashboard.css", html);
    }

    [Fact]
    public async Task StaffUsesAssignedWarehouseAndCannotAdministerCompany()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        var cid = await Seed(owner); using var staff = Browser(f, "staff");
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff Member" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        var products = await staff.GetStringAsync("/Products"); Assert.Contains("Motor", products); Assert.Contains("Main warehouse", products);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Stock/Adjust")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Suppliers/Create")).StatusCode);
    }

    [Fact]
    public async Task ActualWarehouseSwitchRejectsAnOlderOpenForm()
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        var cid = await Seed(client); var oldForm = await Fields(client, "/Stock/Adjust");
        oldForm["ProductId"] = "p"; oldForm["Type"] = "Subtract"; oldForm["Quantity"] = "1"; oldForm["Reason"] = "Correction";
        var selection = await Fields(client, "/Companies"); selection["companyId"] = cid; selection["warehouseId"] = "dest";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Companies/Select", new FormUrlEncodedContent(selection))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(oldForm))).StatusCode);
        Assert.Contains("Destination", await client.GetStringAsync("/Products"));
        var state = (await client.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(7, state.Stock.Sum(x => x.OnHand));
    }

    [Fact]
    public async Task ManualCorrectionReplaysOnceAndExactCountAwaitsAuditApproval()
    {
        await using var f = new Factory(); using var client = Browser(f, "owner");
        var cid = await Seed(client); var form = await Fields(client, "/Stock/Adjust");
        form["ProductId"] = "p"; form["Type"] = "Subtract"; form["Quantity"] = "1"; form["Reason"] = "Correction";
        for (var retry = 0; retry < 2; retry++) Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(form))).StatusCode);
        var count = await Fields(client, "/Stock/Adjust"); count["ProductId"] = "p"; count["Type"] = "Set"; count["Quantity"] = "10"; count["Reason"] = "Counted stock";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(count))).StatusCode);
        var state = (await client.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(6, state.Stock.Sum(x => x.OnHand));
        var audit = Assert.Single(state.Audits); Assert.Equal("Pending", audit.Status); Assert.Equal(10, Assert.Single(audit.Lines).Counted);
    }

}
