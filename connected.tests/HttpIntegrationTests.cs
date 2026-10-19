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
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }, "Test")), "Test")));
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
                s.AddAuthorization(o => o.AddPolicy("CompanyIdentity", new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().Build()));
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
}
