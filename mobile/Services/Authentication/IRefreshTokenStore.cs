namespace Solvix.Sims.Services.Authentication;

public interface IRefreshTokenStore
{
    Task<string?> ReadAsync();
    Task WriteAsync(string token);
    Task ClearAsync();
}

