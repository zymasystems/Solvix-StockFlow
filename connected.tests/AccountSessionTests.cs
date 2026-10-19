using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Connected.Tests;
public sealed class AccountSessionTests
{
    private sealed class SignedIn : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier,"platform-admin"), new Claim("session_expires_at",DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString()) },"Test"))));
    }
    private sealed class Profiles : IConnectedProfiles
    {
        public Task<Profile> GetAsync(string uid) => Task.FromResult(new Profile { Uid=uid });
        public Task SaveAsync(string uid, Operation input) => Task.CompletedTask;
    }
    private sealed class NoBrowserCalls : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => throw new InvalidOperationException("Unexpected browser call.");
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken ct, object?[]? args) => throw new InvalidOperationException("Unexpected browser call.");
    }
    [Fact]
    public async Task ExistingBlazorSessionCannotWriteAfterAccountDeactivation()
    {
        var users = new MemoryUsersRepository(); var companies = new CompanyService(new MemoryStore(), TimeProvider.System);
        var created = await companies.CreateAsync("platform-admin",new() { Name="Session test", RequestId=Guid.NewGuid() });
        var client = new ServerWorkspaceClient(companies,new Profiles(),new SignedIn(),new NoBrowserCalls(),users);
        await client.ExecuteAsync(created.Id,"warehouse.save",new() { Id="first",Name="First",RequestId=Guid.NewGuid() });
        await users.UpdateAdminAsync("platform-admin",new UserEditViewModel { FirstName="Admin",LastName="User",Role="admin",Status="Inactive" });
        var error = await Assert.ThrowsAsync<ServiceException>(()=>client.ExecuteAsync(created.Id,"warehouse.save",new() { Id="second",Name="Second",RequestId=Guid.NewGuid() }));
        Assert.Equal(403,error.StatusCode);
        var state = await companies.LoadAsync("platform-admin",created.Id,"");
        Assert.Equal("first",Assert.Single(state.Warehouses).Id);
    }
}
