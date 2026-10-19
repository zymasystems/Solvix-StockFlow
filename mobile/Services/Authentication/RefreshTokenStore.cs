using Microsoft.Maui.Storage;
namespace Solvix.Sims.Services.Authentication;

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private const string Key = "solvix.firebase.refresh.v1";
    public Task<string?> ReadAsync() => SecureStorage.Default.GetAsync(Key);
    public Task WriteAsync(string token) => SecureStorage.Default.SetAsync(Key, token);
    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(Key);
        // Remove the previous API client's stored ID token during migration/logout.
        SecureStorage.Default.Remove("solvix.api.token");
        return Task.CompletedTask;
    }
}
