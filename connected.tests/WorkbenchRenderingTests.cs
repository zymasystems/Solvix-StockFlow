using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Solvix.Contracts;

namespace Solvix.Connected.Tests;
public sealed class WorkbenchRenderingTests
{
    private sealed class Js : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string id, object? []? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string id, CancellationToken ct, object? []? args) => ValueTask.FromResult(default(TValue)!);
    }

    private sealed class Client : IWorkspaceClient
    {
        private readonly Membership membership = new()
        {
            Company = new()
            {
                Id = "company",
                Name = "Connected company"
            },
            Member = new()
            {
                Id = "u",
                Role = "admin",
                Status = "Active"
            }
        };
        public Task<List<Membership>> CompaniesAsync(CancellationToken ct = default) => Task.FromResult(new List<Membership> { membership });
        public Task<Solvix.Contracts.Workspace> LoadAsync(string c, string w, string? search = null, CancellationToken ct = default) => Task.FromResult(new Solvix.Contracts.Workspace { Company = membership.Company, Permission = membership.Member, Warehouses = new() { new() { Id = "wh", Name = "Main" } }, Products = new() { new() { Id = "p", Name = "Motor", Sku = "ABC", UnitCost = 2.5m } }, Stock = new() { new() { Id = "p", ProductId = "p", WarehouseId = "wh", OnHand = 4 } } });
        public Task<Profile> ProfileAsync(CancellationToken ct = default) => Task.FromResult(new Profile { Uid = "u", FullName = "Owner", Email = "owner@example.test" });
        public Task<Result> CreateCompanyAsync(Operation i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> JoinAsync(string c, Operation i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ExecuteAsync(string c, string cmd, Operation i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveProfileAsync(Operation i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ChangeEmailAsync(string e, string p, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SignOutAsync() => Task.CompletedTask;
    }

    [Theory]
    [InlineData("Inventory", "Warehouse inventory")]
    [InlineData("Receiving", "Submit delivery")]
    [InlineData("Dispatch", "Create dispatch")]
    [InlineData("Transfers", "Transfers")]
    [InlineData("Audits", "Stock audit")]
    [InlineData("Movements", "Movement")]
    [InlineData("Operations", "Employee approval")]
    public async Task EveryOperationsSectionRendersWithBackendData(string section, string expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, Js>();
        services.AddSingleton<IWorkspaceClient, Client>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<Solvix.Workspace.Workbench>(ParameterView.FromDictionary(new Dictionary<string, object?> { { "InitialSection", section } }));
            return root.ToHtmlString();
        });
        Assert.Contains("Connected company", html);
        Assert.Contains(expected, html, StringComparison.OrdinalIgnoreCase);
    }
}
