using Solvix.Contracts;
using Solvix.Sims.Services.Authentication;
using Microsoft.AspNetCore.Components;

namespace Solvix.Sims.Services;
public sealed class MobileWorkspaceClient(ApiService api, IFirebaseAuthService firebase, MobileAuthCoordinator auth, NavigationManager nav) : IWorkspaceClient
{
    private static string Id(string id) => Uri.EscapeDataString(id);
    public async Task<List<Membership>> CompaniesAsync(CancellationToken ct = default) => await api.GetAsync<List<Membership>>("api/me/companies", ct) ?? new();
    public async Task<Solvix.Contracts.Workspace> LoadAsync(string c, string w, string? search = null, CancellationToken ct = default) => await api.GetAsync<Solvix.Contracts.Workspace>($"api/companies/{Id(c)}/workspace?warehouseId={Id(w)}&search={Uri.EscapeDataString(search ?? "")}", ct) ?? throw new ApiException("No workspace returned.");
    public async Task<Result> CreateCompanyAsync(Operation input, CancellationToken ct = default) => await api.PostAsync<Operation, Result>("api/companies", input, ct) ?? throw new ApiException("No company returned.");
    public async Task<Result> JoinAsync(string c, Operation input, CancellationToken ct = default) => await api.PostAsync<Operation, Result>($"api/companies/{Id(c)}/join", input, ct) ?? throw new ApiException("No result returned.");
    public async Task<Result> ExecuteAsync(string c, string command, Operation input, CancellationToken ct = default) => await api.PostAsync<Operation, Result>($"api/companies/{Id(c)}/commands/{Id(command)}", input, ct) ?? throw new ApiException("No result returned.");
    public async Task<Profile> ProfileAsync(CancellationToken ct = default) => await api.GetAsync<Profile>("api/me/profile", ct) ?? throw new ApiException("No profile returned.");
    public async Task SaveProfileAsync(Operation input, CancellationToken ct = default) => await api.PutAsync<Operation, object>("api/me/profile", input, ct);
    public async Task ChangeEmailAsync(string email, string password, CancellationToken ct = default)
    {
        var current = await ProfileAsync(ct);
        await firebase.SignInAsync(current.Email, password, false, ct);
        await firebase.RequestEmailChangeAsync(email, ct);
        await SignOutAsync();
    }

    public async Task SignOutAsync()
    {
        await auth.SignOutAsync();
        nav.NavigateTo("/login", replace: true);
    }
}
