namespace Solvix.Sims.Services.Authentication;

public interface IFirebaseAuthService
{
    string? UserId { get; }
    event Action? SignedOut;
    Task SignInAsync(string email, string password, bool remember, CancellationToken ct = default);
    Task RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<bool> RestoreAsync(CancellationToken ct = default);
    Task<string> GetIdTokenAsync(CancellationToken ct = default);
    Task SendPasswordResetAsync(string email, CancellationToken ct = default);
    Task SignOutAsync();
}
