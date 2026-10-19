using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Solvix.Contracts;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;
public sealed class ServerWorkspaceClient(CompanyService companies, IConnectedProfiles profiles, AuthenticationStateProvider authentication, IJSRuntime js, Solvix.Web.Repositories.Interfaces.IUserRepository accounts) : IWorkspaceClient
{
    private async Task<string> User()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true || !long.TryParse(user.FindFirstValue("session_expires_at"), out var expiry) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiry)
            throw new ServiceException(401, "Website session expired. Sign out and sign in again.");
        var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ServiceException(401, "Sign in again.");
        var profile = await accounts.FindAsync(uid);
        if (profile is null || profile.Status != "Active" || profile.Role is not ("admin" or "staff"))
            throw new ServiceException(403, "Your account is awaiting administrator approval or is inactive.");
        return uid;
    }

    public async Task<List<Membership>> CompaniesAsync(CancellationToken ct = default) => await companies.CompaniesAsync(await User());
    public async Task<Solvix.Contracts.Workspace> LoadAsync(string c, string w, string? search = null, CancellationToken ct = default) => await companies.LoadAsync(await User(), c, w, search);
    public async Task<Result> CreateCompanyAsync(Operation input, CancellationToken ct = default) => await companies.CreateAsync(await User(), input);
    public async Task<Result> JoinAsync(string c, Operation input, CancellationToken ct = default) => await companies.JoinAsync(await User(), c, input);
    public async Task<Result> ExecuteAsync(string c, string command, Operation input, CancellationToken ct = default) => await companies.ExecuteAsync(await User(), c, command, input);
    public async Task<Profile> ProfileAsync(CancellationToken ct = default) => await profiles.GetAsync(await User());
    public async Task SaveProfileAsync(Operation input, CancellationToken ct = default) => await profiles.SaveAsync(await User(), input);
    public async Task ChangeEmailAsync(string email, string password, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(ct);
        await js.InvokeVoidAsync("connectedUi.changeEmail", profile.Email, email, password);
    }

    public async Task SignOutAsync() => await js.InvokeVoidAsync("connectedUi.signOut");
}
