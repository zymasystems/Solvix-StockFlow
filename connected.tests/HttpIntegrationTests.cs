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
                s.RemoveAll<Solvix.Web.Repositories.Interfaces.IUserRepository>();
                s.AddSingleton<MemoryUsersRepository>();
                s.AddSingleton<Solvix.Web.Repositories.Interfaces.IUserRepository>(sp => sp.GetRequiredService<MemoryUsersRepository>());
                s.RemoveAll<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory>();
                s.AddSingleton<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory, MemoryUsersDirectory>();
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
                s.PostConfigure<AuthorizationOptions>(o => {
                    foreach (var name in new[] { "CompanyIdentity", "CompanyAdmin", Solvix.Web.Authorization.Policies.WebAdmin, Solvix.Web.Authorization.Policies.ApiActiveUser })
                    {
                        var actual = o.GetPolicy(name)!;
                        o.AddPolicy(name, new AuthorizationPolicy(actual.Requirements, new[] { "Test" }));
                    }
                    o.FallbackPolicy = new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().Build();
                });
                s.AddAuthentication().AddScheme<AuthenticationSchemeOptions, IdentityHandler>("Test", _ =>
                {
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
            ("company.brand", new() { Name = "Original company", WarehouseId = "wh" }),
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
        Assert.Contains("Find or create your company", html); Assert.Contains("main warehouse automatically", html);
    }

    [Fact]
    public async Task ApprovedUserCreatingCompanyThroughWebsiteSeesRegisteredName()
    {
        await using var f = new Factory();
        using var client = Browser(f, "new-user");
        var accounts = f.Services.GetRequiredService<MemoryUsersRepository>();
        await accounts.UpdateNamesAsync("new-user", "Mashaole", "Letsoalo");
        var form = await Fields(client, "/Companies");
        form["name"] = "New business";
        form["requestId"] = Guid.NewGuid().ToString();

        var response = await client.PostAsync("/Companies/Create", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var html = await client.GetStringAsync("/Dashboard");
        Assert.Contains("Welcome, Mashaole Letsoalo</h1>", html);
        Assert.DoesNotContain("Welcome, new-user</h1>", html);
        Assert.DoesNotContain("Manage users", html); // Company ownership does not grant account administration.
        var membership = Assert.Single((await client.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.Equal("Mashaole Letsoalo", membership.Member.FullName);
        Assert.Equal("new-user@example.test", membership.Member.Email);
        Assert.Equal("admin", membership.Member.Role);
    }

    [Fact]
    public async Task ExistingUidMembershipUsesCurrentAccountNameWithoutRewritingMembership()
    {
        await using var f = new Factory();
        using var client = Browser(f, "owner");
        var cid = await Seed(client);
        var store = f.Services.GetRequiredService<ICompanyStore>();
        await store.RunAsync(async tx =>
        {
            var path = $"companies/{cid}/members/owner";
            var member = (await tx.GetAsync<Member>(path))!;
            member.FullName = member.Id;
            tx.Put(path, member);
            return true;
        });

        Assert.Contains("Welcome, Owner Admin</h1>", await client.GetStringAsync("/Dashboard"));
        await f.Services.GetRequiredService<MemoryUsersRepository>().UpdateNamesAsync("owner", "Updated", "Person");
        Assert.Contains("Welcome, Updated Person</h1>", await client.GetStringAsync("/Dashboard"));
        Assert.Equal("owner", (await store.GetAsync<Member>($"companies/{cid}/members/owner"))!.FullName);
    }

    [Fact]
    public async Task ApiCompanyOwnerNameComesFromAccountAndUidNameFallsBackToEmail()
    {
        await using var f = new Factory();
        using var client = Browser(f, "owner");
        var created = await client.PostAsJsonAsync("/api/companies", new Operation
        {
            RequestId = Guid.NewGuid(), Name = "API business", Reference = "Caller-supplied name"
        });
        created.EnsureSuccessStatusCode();
        var cid = (await created.Content.ReadFromJsonAsync<Result>())!.Id;
        var state = (await client.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal("Owner Admin", state.Permission.FullName);
        Assert.Equal("owner@example.test", state.Permission.Email);

        await f.Services.GetRequiredService<MemoryUsersRepository>().UpdateNamesAsync("owner", "owner", "");
        Assert.Contains("Welcome, owner@example.test</h1>", await client.GetStringAsync("/Dashboard"));
        Assert.DoesNotContain("Welcome, owner</h1>", await client.GetStringAsync("/Dashboard"));
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
        Assert.Contains("Stock approvals", html);
        Assert.Contains("Pending stock receipts", html);
        Assert.DoesNotContain("blazor.server.js", html);
        Assert.Contains("dashboard.css", html);
    }

    [Fact]
    public async Task StaffUsesAssignedWarehouseAndCannotAdministerCompany()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        var cid = await Seed(owner); using var staff = Browser(f, "staff");
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff Member" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        var products = await staff.GetStringAsync("/Products"); Assert.Contains("Motor", products); Assert.DoesNotContain("Manage users</a>", products);
        Assert.Equal("wh", (await Fields(staff, "/Products/Create"))["WarehouseContextId"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/Stock/Adjust")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Suppliers/Create")).StatusCode);
    }

    [Theory]
    [InlineData("Add", "3", 10)]
    [InlineData("Subtract", "2", 5)]
    [InlineData("Set", "11", 11)]
    public async Task StaffAdjustmentWaitsForAdminApproval(string type, string quantity, long expected)
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        var cid = await Seed(owner); using var staff = Browser(f, "staff");
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff Member" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        var form = await Fields(staff, "/Stock/Adjust");
        form["ProductId"] = "p"; form["Type"] = type; form["Quantity"] = quantity; form["Reason"] = "Physical correction";
        for (var retry = 0; retry < 2; retry++)
            Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(7, state.Stock.Sum(x => x.OnHand));
        var audit = Assert.Single(state.Audits); Assert.Equal(expected, Assert.Single(audit.Lines).Counted);
        Assert.Equal("Physical correction", audit.Notes);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync($"/api/companies/{cid}/commands/audit.approve", new Operation { RequestId = Guid.NewGuid(), Id = audit.Id })).StatusCode);
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/audit.approve", new Operation { RequestId = Guid.NewGuid(), Id = audit.Id })).EnsureSuccessStatusCode();
        state = (await staff.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(expected, state.Stock.Sum(x => x.OnHand));
    }

    [Fact]
    public async Task ProductSupplierSelectionPersistsAndRejectsForeignSupplier()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        var cid = await Seed(owner);
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/supplier.save", new Operation { RequestId = Guid.NewGuid(), Id = "supplier1", Name = "Motor Supply" })).EnsureSuccessStatusCode();
        var form = await Fields(owner, "/Products/Edit/p");
        Assert.Contains("Motor Supply", await owner.GetStringAsync("/Products/Edit/p"));
        form["Name"] = "Motor"; form["Sku"] = "ABC"; form["Category"] = "Electronics"; form["Price"] = "5.00"; form["MinStock"] = "0"; form["SupplierId"] = "supplier1";
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Products/Edit/p", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal("supplier1", state.Products.Single().SupplierId);
        var html = await owner.GetStringAsync("/Products/Edit/p"); Assert.Contains("selected=", html);
        var rejected = await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/product.save", new Operation { RequestId = Guid.NewGuid(), Id = "p", Name = "Motor", Sku = "ABC", SupplierId = "foreign" });
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
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
        Assert.Equal("dest", (await Fields(client, "/Products/Create"))["WarehouseContextId"]);
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
        Assert.Equal(7, state.Stock.Sum(x => x.OnHand));
        Assert.Equal(2, state.Audits.Count); Assert.All(state.Audits, a => Assert.Equal("Pending", a.Status));
        Assert.Contains(state.Audits, a => Assert.Single(a.Lines).Counted == 6);
        Assert.Contains(state.Audits, a => Assert.Single(a.Lines).Counted == 10);
    }

    [Fact]
    public async Task ExistingAdminSeesAndApprovesAccountsWithoutCompanyMembership()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin"); using var pending = Browser(f, "pending");
        Assert.Empty((await admin.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        var companies = await admin.GetStringAsync("/Companies");
        Assert.Contains("Manage users</a>", companies);
        var users = await admin.GetStringAsync("/Users"); Assert.Contains("pending@example.test", users); Assert.Contains("Inactive", users);
        Assert.Equal(HttpStatusCode.Forbidden, (await pending.GetAsync("/api/me/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pending.GetAsync("/api/profile")).StatusCode);
        var form = await Fields(admin, "/Users/Edit/pending");
        form["FirstName"] = "Pending"; form["LastName"] = "User"; form["Company"] = "Listed company"; form["Role"] = "staff"; form["Status"] = "Active";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Edit/pending", new FormUrlEncodedContent(form))).StatusCode);
        var saved = await f.Services.GetRequiredService<MemoryUsersRepository>().FindAsync("pending");
        Assert.Equal("Active", saved!.Status); Assert.Equal("staff", saved.Role);
        var assigned = Assert.Single((await pending.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.Equal("Listed company", assigned.Company.Name);
        Assert.Equal("staff", assigned.Member.Role);
        Assert.Equal("Active", assigned.Member.Status);
        Assert.Contains(assigned.Company.DefaultWarehouseId, assigned.Member.WarehouseIds);
        form["Status"] = "Inactive";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Edit/pending", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pending.GetAsync("/api/me/companies")).StatusCode); // Already-issued identity is checked again.
    }

    [Fact]
    public async Task CompanyAdminCannotApproveGlobalAccountsOrPromoteSelf()
    {
        await using var f = new Factory(); using var owner = Browser(f, "company-owner");
        await Seed(owner); // This user is an approved staff account and a company administrator only.
        var html = await owner.GetStringAsync("/Products");
        Assert.DoesNotContain("Manage users</a>", html);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsync("/Users/Edit/company-owner", new FormUrlEncodedContent(new Dictionary<string,string> { ["Role"]="admin", ["Status"]="Active" }))).StatusCode);
        Assert.Equal("staff", (await f.Services.GetRequiredService<MemoryUsersRepository>().FindAsync("company-owner"))!.Role);
    }

    [Fact]
    public async Task AccountAdminCannotRemoveOwnAdministratorAccess()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin");
        var form = await Fields(admin, "/Users/Edit/platform-admin"); form["Role"] = "staff"; form["Status"] = "Inactive";
        var response = await admin.PostAsync("/Users/Edit/platform-admin", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You cannot remove your own administrator access.", await response.Content.ReadAsStringAsync());
        var profile = await f.Services.GetRequiredService<MemoryUsersRepository>().FindAsync("platform-admin"); Assert.Equal("admin", profile!.Role); Assert.Equal("Active", profile.Status);
    }

    [Fact]
    public async Task AdminDeletionRequiresConfirmationAndKeepsInventoryHistory()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        using var admin = Browser(f, "platform-admin"); using var staff = Browser(f, "staff");
        var cid = await Seed(owner);
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff Member" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        var before = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        var users = f.Services.GetRequiredService<MemoryUsersRepository>();
        var directory = (MemoryUsersDirectory)f.Services.GetRequiredService<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory>();

        var html = await admin.GetStringAsync("/Users");
        Assert.Contains("/Users/Delete/staff", html);
        Assert.DoesNotContain("/Users/Delete/platform-admin", html);
        var form = await Fields(admin, "/Users/Delete/staff");
        Assert.Equal("Active", (await users.FindAsync("staff"))!.Status); // GET never deletes or deactivates.
        Assert.Empty(directory.DeletedUsers);
        directory.BeforeDelete = async uid =>
        {
            Assert.Equal("Inactive", (await users.FindAsync(uid))!.Status);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/profile")).StatusCode);
        };
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Null(await users.FindAsync("staff"));
        Assert.Contains("staff", directory.DeletedUsers.Keys);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync($"/api/companies/{cid}/workspace")).StatusCode);
        var after = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal(before.Stock.Sum(s => s.OnHand), after.Stock.Sum(s => s.OnHand));
        Assert.Equal(before.Movements.Count, after.Movements.Count);
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form))).StatusCode);
    }

    [Fact]
    public async Task StaffAndCompanyAdministratorCannotDeleteAccounts()
    {
        await using var f = new Factory(); using var staff = Browser(f, "staff"); using var companyAdmin = Browser(f, "company-owner");
        await Seed(companyAdmin);
        foreach (var client in new[] { staff, companyAdmin })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Users/Delete/other")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/Users/Delete/other", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        }
        Assert.NotNull(await f.Services.GetRequiredService<MemoryUsersRepository>().FindAsync("other"));
    }

    [Fact]
    public async Task AdminDeleteRejectsMissingAntiforgeryTokenAndSelfDeletion()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin");
        var users = f.Services.GetRequiredService<MemoryUsersRepository>();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        Assert.Equal("Active", (await users.FindAsync("staff"))!.Status);
        var html = await admin.GetStringAsync("/Users/Delete/platform-admin");
        Assert.DoesNotContain("Permanently delete user</button>", html);
        var form = await Fields(admin, "/Users/Edit/platform-admin");
        var response = await admin.PostAsync("/Users/Delete/platform-admin", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You cannot delete your own administrator account.", await response.Content.ReadAsStringAsync());
        Assert.Equal("Active", (await users.FindAsync("platform-admin"))!.Status);
        Assert.Empty(((MemoryUsersDirectory)f.Services.GetRequiredService<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory>()).DeletedUsers);
    }

    [Fact]
    public async Task FirebaseDeleteFailureBlocksAccessAndReactivationUntilRetry()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin"); using var staff = Browser(f, "staff");
        var users = f.Services.GetRequiredService<MemoryUsersRepository>();
        var directory = (MemoryUsersDirectory)f.Services.GetRequiredService<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory>();
        directory.FailDeletion = true;
        var form = await Fields(admin, "/Users/Delete/staff");
        var failed = await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
        Assert.Contains("Retry deleting this user.", await failed.Content.ReadAsStringAsync());
        Assert.Equal("Inactive", (await users.FindAsync("staff"))!.Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/profile")).StatusCode);
        var edit = await Fields(admin, "/Users/Edit/staff"); edit["FirstName"] = "Staff"; edit["LastName"] = "User"; edit["Role"] = "staff"; edit["Status"] = "Active";
        var blocked = await admin.PostAsync("/Users/Edit/staff", new FormUrlEncodedContent(edit));
        Assert.Contains("Retry deletion instead of editing or reactivating it.", await blocked.Content.ReadAsStringAsync());
        Assert.Equal("Inactive", (await users.FindAsync("staff"))!.Status);
        directory.FailDeletion = false;
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Null(await users.FindAsync("staff"));
    }

    [Fact]
    public async Task ProfileCleanupCanBeRetriedAfterFirebaseAccountWasDeleted()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin");
        var users = f.Services.GetRequiredService<MemoryUsersRepository>();
        var directory = (MemoryUsersDirectory)f.Services.GetRequiredService<Solvix.Web.Services.Interfaces.IFirebaseUserDirectory>();
        users.FailProfileDeletion = true;
        var form = await Fields(admin, "/Users/Delete/staff");
        var response = await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form));
        Assert.Contains("Profile cleanup failed. Retry deletion.", await response.Content.ReadAsStringAsync());
        Assert.Contains("staff", directory.DeletedUsers.Keys);
        Assert.Equal("Inactive", (await users.FindAsync("staff"))!.Status);
        users.FailProfileDeletion = false;
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Users/Delete/staff", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Null(await users.FindAsync("staff"));
    }

    [Fact]
    public async Task ProductIntakeTargetsOnlySelectedWarehouseAndRejectsForeignWarehouse()
    {
        await using var f = new Factory(); using var admin = Browser(f, "owner");
        var cid = await Seed(admin);
        var html = await admin.GetStringAsync("/Products/Create");
        Assert.Contains("name=\"WarehouseId\"", html);
        var form = await Fields(admin, "/Products/Create");
        form["Name"] = "Destination product"; form["Sku"] = "TARGET"; form["Category"] = "Electronics";
        form["Price"] = "10"; form["MinStock"] = "4"; form["OpeningStock"] = "3"; form["WarehouseId"] = "dest";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Products/Create", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await admin.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        var product = Assert.Single(state.Products, p => p.Sku == "TARGET");
        Assert.Equal("dest", Assert.Single(state.Stock, s => s.ProductId == product.Id).WarehouseId);
        Assert.Equal("dest", Assert.Single(state.Receipts).WarehouseId);
        Assert.Equal(0, state.Stock.Single(s => s.ProductId == product.Id).OnHand); // Approval still required.
        var other = await admin.PostAsJsonAsync("/api/companies", new Operation { Name = "Pharma", RequestId = Guid.NewGuid() });
        var otherId = (await other.Content.ReadFromJsonAsync<Result>())!.Id;
        (await admin.PostAsJsonAsync($"/api/companies/{otherId}/commands/warehouse.save", new Operation { Id = "foreign", Name = "Pharma only", RequestId = Guid.NewGuid() })).EnsureSuccessStatusCode();
        form = await Fields(admin, "/Products/Create");
        form["Name"] = "Must not save"; form["Sku"] = "BLOCK"; form["Category"] = "Electronics";
        form["Price"] = "10"; form["MinStock"] = "1"; form["OpeningStock"] = "0"; form["WarehouseId"] = "foreign";
        var rejected = await admin.PostAsync("/Products/Create", new FormUrlEncodedContent(form));
        Assert.Contains("Choose an accessible warehouse", await rejected.Content.ReadAsStringAsync());
        Assert.DoesNotContain((await admin.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!.Products, p => p.Sku == "BLOCK");
        Assert.Empty((await admin.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{otherId}/workspace"))!.Products);
    }

    [Fact]
    public async Task SystemAdminCompanyPanelShowsOtherCompaniesAndCompanyAdminCannotSeeTheirMembers()
    {
        await using var f = new Factory(); using var creator = Browser(f, "company-owner"); using var admin = Browser(f, "platform-admin"); using var unrelated = Browser(f, "new-user");
        var cid = await Seed(creator);
        var panel = await admin.GetStringAsync("/Companies");
        Assert.Contains("Original company", panel);
        Assert.Contains("Members and roles", panel);
        Assert.Contains("Company registered:", panel);
        Assert.Contains("name=\"warehouseIds\"", panel);
        Assert.DoesNotContain("Original company", await unrelated.GetStringAsync("/Companies"));
        var form = await Fields(admin, "/Companies"); form["companyId"] = cid; form["warehouseId"] = "wh";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Companies/Select", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Contains("Original company", await admin.GetStringAsync("/Products/Create"));
        // A company owner still has no system-wide account-approval rights.
        Assert.Equal(HttpStatusCode.Forbidden, (await creator.GetAsync("/Users")).StatusCode);
    }

    [Fact]
    public async Task CompanyDeletionRequiresSystemAdminAntiforgeryAndNameConfirmation()
    {
        await using var f = new Factory(); using var owner = Browser(f, "company-owner"); using var admin = Browser(f, "platform-admin");
        var cid = await Seed(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/Companies/Delete/{cid}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/Companies/Delete/{cid}", new FormUrlEncodedContent(new Dictionary<string,string> { ["confirmationName"] = "Original company" }))).StatusCode);
        var store = f.Services.GetRequiredService<ICompanyStore>();
        var form = await Fields(admin, $"/Companies/Delete/{cid}");
        Assert.NotNull(await store.GetAsync<Company>($"companies/{cid}/meta/company")); // GET is read-only.
        form["ConfirmationName"] = "Wrong";
        var rejected = await admin.PostAsync($"/Companies/Delete/{cid}", new FormUrlEncodedContent(form));
        Assert.Contains("Type the company name exactly", await rejected.Content.ReadAsStringAsync());
        Assert.Null(await store.GetAsync<CompanyDeletionJob>("companyDeletions/" + cid));
        form["ConfirmationName"] = "Original company"; form["DeleteExclusiveAccounts"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync($"/Companies/Delete/{cid}", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Null(await store.GetAsync<Company>($"companies/{cid}/meta/company"));
        Assert.Empty((await owner.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.NotNull(await f.Services.GetRequiredService<MemoryUsersRepository>().FindAsync("company-owner"));
        Assert.Equal(HttpStatusCode.Gone, (await owner.GetAsync($"/api/companies/{cid}/workspace")).StatusCode);
        Assert.Contains("already been deleted", await admin.GetStringAsync($"/Companies/Delete/{cid}"));
    }

    [Fact]
    public async Task SystemAdminWithoutMembershipCanNavigateAnAvailableCompany()
    {
        await using var f = new Factory(); using var creator = Browser(f, "company-owner"); using var admin = Browser(f, "platform-admin");
        var cid = await Seed(creator);
        Assert.Empty((await admin.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        var products = await admin.GetAsync("/Products");
        Assert.Equal(HttpStatusCode.OK, products.StatusCode);
        Assert.Contains("Active company: Original company", await products.Content.ReadAsStringAsync());
        foreach (var route in new[] { "/Suppliers", "/Reports", "/Dashboard" })
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(route)).StatusCode);
        var membership = Assert.Single((await admin.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.Equal(cid, membership.Company.Id);
        Assert.Equal("admin", membership.Member.Role);
    }

    [Fact]
    public async Task DeletingSelectedCompanyFallsBackToAnotherAvailableCompany()
    {
        await using var f = new Factory(); using var creator = Browser(f, "company-owner"); using var admin = Browser(f, "platform-admin");
        var cid = await Seed(creator);
        var created = await creator.PostAsJsonAsync("/api/companies", new Operation { Name = "Zara Systems", RequestId = Guid.NewGuid() });
        var other = (await created.Content.ReadFromJsonAsync<Result>())!.Id;
        (await creator.PostAsJsonAsync($"/api/companies/{other}/commands/product.save", new Operation { Name = "Zara stock", Sku = "ZARA", RequestId = Guid.NewGuid() })).EnsureSuccessStatusCode();
        Assert.Contains("Motor", await admin.GetStringAsync("/Products"));
        var form = await Fields(admin, $"/Companies/Delete/{cid}");
        form["ConfirmationName"] = "Original company"; form["DeleteExclusiveAccounts"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync($"/Companies/Delete/{cid}", new FormUrlEncodedContent(form))).StatusCode);
        var products = await admin.GetAsync("/Products");
        Assert.Equal(HttpStatusCode.OK, products.StatusCode);
        Assert.Contains("Zara stock", await products.Content.ReadAsStringAsync());
        Assert.Equal(other, Assert.Single((await admin.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!).Company.Id);
    }

    [Fact]
    public async Task NoCompanyShowsSetupAndFallbackNeverGrantsStaffOrRunsOnPost()
    {
        await using var f = new Factory(); using var admin = Browser(f, "platform-admin"); using var creator = Browser(f, "company-owner"); using var staff = Browser(f, "new-user");
        Assert.Equal(HttpStatusCode.Redirect, (await admin.GetAsync("/Products")).StatusCode);
        var form = await Fields(admin, "/Companies");
        var setup = await admin.GetStringAsync("/Companies");
        Assert.Contains("Set up company access to use inventory", setup);
        Assert.Contains("href=\"#company-setup\"", setup);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Users")).StatusCode);
        await Seed(creator);
        // An existing stale form must not silently enroll the administrator or switch its write target.
        var post = await admin.PostAsync("/Products/Create", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.Equal(HttpStatusCode.Redirect, (await staff.GetAsync("/Products")).StatusCode);
        Assert.Empty((await staff.GetFromJsonAsync<List<Membership>>("/api/me/companies"))!);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Products")).StatusCode);
    }

    private static async Task<Dictionary<string,string>> ApprovalForm(HttpClient client, string action, string id)
    {
        var html = await client.GetStringAsync("/Stock/Approvals");
        var forms = System.Text.RegularExpressions.Regex.Matches(html, "<form[^>]*>.*?</form>", System.Text.RegularExpressions.RegexOptions.Singleline);
        var form = forms.Cast<System.Text.RegularExpressions.Match>().First(m => m.Value.Contains("action=\"/Stock/" + action + "\"") && m.Value.Contains("value=\"" + id + "\""));
        var fields = new Dictionary<string,string>();
        foreach(System.Text.RegularExpressions.Match input in System.Text.RegularExpressions.Regex.Matches(form.Value, "<input[^>]+>"))
        {
            var name = System.Text.RegularExpressions.Regex.Match(input.Value, "name=\"([^\"]+)\"");
            var value = System.Text.RegularExpressions.Regex.Match(input.Value, "value=\"([^\"]*)\"");
            if(name.Success && value.Success) fields[WebUtility.HtmlDecode(name.Groups[1].Value)] = WebUtility.HtmlDecode(value.Groups[1].Value);
        }
        return fields;
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("staff")]
    public async Task WebsiteOpeningStockApprovalThenStaffAdjustmentWorksWithoutInteractiveCircuit(string creatorUid)
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner"); using var staff = Browser(f, "staff");
        var cid = await Seed(owner);
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff Member" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        var creator = creatorUid == "owner" ? owner : staff;
        var productForm = await Fields(creator, "/Products/Create");
        productForm["Name"] = "New component"; productForm["Sku"] = "NEW"; productForm["Category"] = "Electronics"; productForm["Price"] = "10";
        productForm["WarehouseId"] = "wh"; productForm["OpeningStock"] = "12"; productForm["MinStock"] = "1";
        Assert.Equal(HttpStatusCode.Redirect, (await creator.PostAsync("/Products/Create", new FormUrlEncodedContent(productForm))).StatusCode);
        async Task<Solvix.Contracts.Workspace> State() => (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace?warehouseId=wh"))!;
        var state = await State(); var product = state.Products.Single(p => p.Sku == "NEW"); var receipt = Assert.Single(state.Receipts);
        Assert.Equal(0, state.Stock.Single(s => s.ProductId == product.Id).OnHand);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync("/Stock/ReviewReceipt", new FormUrlEncodedContent(new Dictionary<string,string> { ["id"] = receipt.Id, ["approve"] = "true" }))).StatusCode);
        var early = await Fields(owner, "/Stock/Approvals"); early["id"] = receipt.Id; early["approve"] = "true"; early["requestId"] = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewReceipt", new FormUrlEncodedContent(early))).StatusCode);
        Assert.Equal("Pending", Assert.Single((await State()).Receipts).Status);
        var arrival = await ApprovalForm(staff, "ConfirmArrival", receipt.Id);
        Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/Stock/ConfirmArrival", new FormUrlEncodedContent(arrival))).StatusCode);
        var verify = await ApprovalForm(staff, "VerifyReceipt", receipt.Id); verify["barcode"] = "WRONG";
        Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/Stock/VerifyReceipt", new FormUrlEncodedContent(verify))).StatusCode);
        Assert.False(Assert.Single((await State()).Receipts).Verified);
        verify = await ApprovalForm(staff, "VerifyReceipt", receipt.Id); verify["barcode"] = "NEW";
        Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/Stock/VerifyReceipt", new FormUrlEncodedContent(verify))).StatusCode);
        var approve = await ApprovalForm(owner, "ReviewReceipt", receipt.Id); approve["approve"] = "true";
        for(var retry=0;retry<2;retry++) Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewReceipt", new FormUrlEncodedContent(approve))).StatusCode);
        Assert.Equal(12, (await State()).Stock.Single(s => s.ProductId == product.Id).OnHand);
        var adjust = await Fields(staff, "/Stock/Adjust"); adjust["ProductId"] = product.Id; adjust["Type"] = "Subtract"; adjust["Quantity"] = "2"; adjust["Reason"] = "Damaged";
        Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(adjust))).StatusCode);
        state = await State(); Assert.Equal(12, state.Stock.Single(s => s.ProductId == product.Id).OnHand);
        var audit = Assert.Single(state.Audits); var review = await ApprovalForm(owner, "ReviewAudit", audit.Id); review["approve"] = "true";
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsync("/Stock/ReviewAudit", new FormUrlEncodedContent(review))).StatusCode);
        for(var retry=0;retry<2;retry++) Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewAudit", new FormUrlEncodedContent(review))).StatusCode);
        Assert.Equal(10, (await State()).Stock.Single(s => s.ProductId == product.Id).OnHand);
        Assert.Contains("Damaged", await staff.GetStringAsync("/Stock/History"));
    }

    [Fact]
    public async Task ProductLocationPickerListsCompanyWarehousesAndKeepsTenantWritesSeparate()
    {
        await using var f = new Factory(); using var firstOwner = Browser(f, "company-owner"); using var secondOwner = Browser(f, "other"); using var admin = Browser(f, "platform-admin");
        var cid = await Seed(firstOwner);
        var created = await secondOwner.PostAsJsonAsync("/api/companies", new Operation { Name = "ePips", RequestId = Guid.NewGuid() }); created.EnsureSuccessStatusCode();
        var other = (await created.Content.ReadFromJsonAsync<Result>())!.Id;
        var html = await admin.GetStringAsync("/Products/Create");
        Assert.Contains("Original company · Main warehouse", html); Assert.Contains("Original company · Destination", html); Assert.Contains("ePips · ePips Main warehouse", html);
        var select = await Fields(admin, "/Products/Create"); select["location"] = other + "/main"; select["returnPage"] = "ProductCreate";
        var response = await admin.PostAsync("/Companies/SelectLocation", new FormUrlEncodedContent(select));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal("/Products/Create", response.Headers.Location?.OriginalString);
        var product = await Fields(admin, "/Products/Create"); Assert.Equal(other, product["CompanyContextId"]); Assert.Equal("main", product["WarehouseContextId"]);
        product["Name"] = "ePips only"; product["Sku"] = "EPIPS"; product["Category"] = "Electronics"; product["Price"] = "5"; product["WarehouseId"] = "main"; product["OpeningStock"] = "0"; product["MinStock"] = "0";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Products/Create", new FormUrlEncodedContent(product))).StatusCode);
        Assert.DoesNotContain("ePips only", await firstOwner.GetStringAsync("/Products"));
        Assert.Contains("ePips only", await secondOwner.GetStringAsync("/Products"));
        Assert.DoesNotContain("ePips ·", await firstOwner.GetStringAsync("/Products/Create"));
        var forbidden = await Fields(firstOwner, "/Products/Create"); forbidden["location"] = other + "/main";
        Assert.Equal(HttpStatusCode.Forbidden, (await firstOwner.PostAsync("/Companies/SelectLocation", new FormUrlEncodedContent(forbidden))).StatusCode);
    }

    [Fact]
    public async Task PortalShowsStaleAdjustmentAndAllowsRejectionWithoutPosting()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner");
        var cid = await Seed(owner); var form = await Fields(owner, "/Stock/Adjust");
        form["ProductId"] = "p"; form["Type"] = "Add"; form["Quantity"] = "2"; form["Reason"] = "Count correction";
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/Adjust", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        var audit = Assert.Single(state.Audits); var oldForm = await ApprovalForm(owner, "ReviewAudit", audit.Id); oldForm["approve"] = "true";
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/stock.adjust", new Operation { RequestId = Guid.NewGuid(), ProductId = "p", WarehouseId = "wh", Quantity = 3, Notes = "Other approved change" })).EnsureSuccessStatusCode();
        Assert.Contains("Stock changed after this request was submitted", await owner.GetStringAsync("/Stock/Approvals"));
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewAudit", new FormUrlEncodedContent(oldForm))).StatusCode);
        state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal("Pending", Assert.Single(state.Audits).Status); Assert.Equal(10, state.Stock.Sum(s => s.OnHand));
        var reject = await ApprovalForm(owner, "ReviewAudit", audit.Id); reject["approve"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewAudit", new FormUrlEncodedContent(reject))).StatusCode);
        state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal("Rejected", Assert.Single(state.Audits).Status); Assert.Equal(10, state.Stock.Sum(s => s.OnHand));
        var delivery = await Fields(owner, "/Receiving"); delivery["productId"] = "p"; delivery["quantity"] = "4"; delivery["reference"] = "PO-1"; delivery["requestId"] = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/SubmitReceipt", new FormUrlEncodedContent(delivery))).StatusCode);
        var receipt = Assert.Single((await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!.Receipts);
        reject = await ApprovalForm(owner, "ReviewReceipt", receipt.Id); reject["approve"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/Stock/ReviewReceipt", new FormUrlEncodedContent(reject))).StatusCode);
        state = (await owner.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{cid}/workspace"))!;
        Assert.Equal("Rejected", Assert.Single(state.Receipts).Status); Assert.Equal(10, state.Stock.Sum(s => s.OnHand));
    }

    [Fact]
    public async Task SidebarApprovalVisibilityAndHighlightFollowCompanyRoleAndAction()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner"); using var staff = Browser(f, "staff");
        var cid = await Seed(owner);
        (await staff.PostAsJsonAsync($"/api/companies/{cid}/join", new Operation { Name = "Staff" })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/companies/{cid}/commands/member.review", new Operation { RequestId = Guid.NewGuid(), Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "wh" } })).EnsureSuccessStatusCode();
        Assert.DoesNotContain("Stock approvals</a>", await staff.GetStringAsync("/Stock/History"));
        var html = await owner.GetStringAsync("/Stock/Approvals");
        var active = System.Text.RegularExpressions.Regex.Matches(html, "<a[^>]*class=\"nav-item active\"[^>]*>.*?</a>", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Single(active); Assert.Contains("Stock approvals", active[0].Value); Assert.Contains("aria-current=\"page\"", active[0].Value);
        html = await owner.GetStringAsync("/Stock/History");
        active = System.Text.RegularExpressions.Regex.Matches(html, "<a[^>]*class=\"nav-item active\"[^>]*>.*?</a>", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Single(active); Assert.Contains("Stock adjustments", active[0].Value);
    }

    [Fact]
    public async Task SupplierCreationSelectsCompanyAndOptionalWarehouseAndPreventsForeignWarehouse()
    {
        await using var f = new Factory(); using var owner = Browser(f, "owner"); using var other = Browser(f, "other"); using var admin = Browser(f, "platform-admin");
        var first = await Seed(owner);
        var created = await other.PostAsJsonAsync("/api/companies", new Operation { Name = "ePips", RequestId = Guid.NewGuid() }); created.EnsureSuccessStatusCode();
        var second = (await created.Content.ReadFromJsonAsync<Result>())!.Id;
        var html = await admin.GetStringAsync("/Suppliers/Create"); Assert.Contains("ePips · ePips Main warehouse", html);
        var select = await Fields(admin, "/Suppliers/Create"); select["location"] = second + "/main"; select["returnPage"] = "SupplierCreate";
        var result = await admin.PostAsync("/Companies/SelectLocation", new FormUrlEncodedContent(select));
        Assert.Equal("/Suppliers/Create", result.Headers.Location?.OriginalString);
        var form = await Fields(admin, "/Suppliers/Create"); form["Name"] = "ePips Supplier"; form["ContactPerson"] = "Jane Smith"; form["Email"] = "supplier@example.test"; form["Phone"] = "+27123456789"; form["Status"] = "Active"; form["Rating"] = "4"; form["WarehouseId"] = "main";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Suppliers/Create", new FormUrlEncodedContent(form))).StatusCode);
        var state = (await other.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{second}/workspace"))!;
        Assert.Equal("main", Assert.Single(state.Suppliers).WarehouseId);
        Assert.DoesNotContain("ePips Supplier", await owner.GetStringAsync("/Suppliers"));
        var wrong = await admin.PostAsJsonAsync($"/api/companies/{second}/commands/supplier.save", new Operation { RequestId = Guid.NewGuid(), Name = "Wrong", WarehouseId = "wh" });
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        form = await Fields(admin, "/Suppliers/Create"); form["Name"] = "Company-wide"; form["ContactPerson"] = "Jane Smith"; form["Email"] = "company@example.test"; form["Phone"] = "+27123456789"; form["Status"] = "Active"; form["Rating"] = "4"; form["WarehouseId"] = "";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/Suppliers/Create", new FormUrlEncodedContent(form))).StatusCode);
        state = (await other.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{second}/workspace"))!;
        Assert.Null(state.Suppliers.Single(x => x.Name == "Company-wide").WarehouseId);
        (await other.PostAsJsonAsync($"/api/companies/{second}/commands/warehouse.save", new Operation { RequestId = Guid.NewGuid(), Id = "satellite", Name = "Satellite" })).EnsureSuccessStatusCode();
        var scoped = (await other.GetFromJsonAsync<Solvix.Contracts.Workspace>($"/api/companies/{second}/workspace?warehouseId=satellite"))!;
        Assert.DoesNotContain(scoped.Suppliers, x => x.Name == "ePips Supplier");
        Assert.Contains(scoped.Suppliers, x => x.Name == "Company-wide");
    }

}
