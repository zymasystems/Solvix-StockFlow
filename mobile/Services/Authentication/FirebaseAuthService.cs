using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
namespace Solvix.Sims.Services.Authentication;

/// <summary>Email/password Firebase REST client. Only refresh tokens may be persisted.</summary>
public sealed class FirebaseAuthService : IFirebaseAuthService
{
    private readonly HttpClient http;
    private readonly IRefreshTokenStore store;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly TimeProvider clock;
    private FirebaseAuthOptions? options;
    private readonly Func<Task<FirebaseAuthOptions>> loadOptions;
    private string? idToken, refreshToken;
    private DateTimeOffset expiresAt;
    private bool persistent;
    public string? UserId { get; private set; }
    public event Action? SignedOut;

    public FirebaseAuthService(HttpClient http, IRefreshTokenStore store,
        Func<Task<FirebaseAuthOptions>> loadOptions, TimeProvider? clock = null)
    {
        this.http = http; this.store = store; this.loadOptions = loadOptions;
        this.clock = clock ?? TimeProvider.System;
    }
    private async Task<FirebaseAuthOptions> OptionsAsync() => options ??= await loadOptions();

    public Task SignInAsync(string email, string password, bool remember, CancellationToken ct = default)
        => AuthenticateAsync("signInWithPassword", email, password, remember, ct);
    public Task RegisterAsync(string email, string password, CancellationToken ct = default)
        => AuthenticateAsync("signUp", email, password, false, ct);

    private async Task AuthenticateAsync(string operation, string email, string password, bool remember, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            throw new AuthException("INPUT_REQUIRED", "Enter your email and password.");
        await gate.WaitAsync(ct);
        try
        {
            await ClearCoreAsync(); // Never fall back to another user's old session after a failed sign-in.
            var o = await OptionsAsync();
            using var response = await http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:{operation}?key={Uri.EscapeDataString(o.ApiKey)}",
                new { email = email.Trim(), password, returnSecureToken = true }, ct);
            using var body = await ReadAsync(response, ct);
            var json = body.RootElement;
            var newId = Required(json, "idToken");
            var newRefresh = Required(json, "refreshToken");
            var uid = Required(json, "localId");
            var expiry = Expiry(Required(json, "expiresIn"));
            if (remember)
            {
                try { await store.WriteAsync(newRefresh); }
                catch { throw new AuthException("STORAGE_FAILED", "Secure sign-in storage is unavailable. Try again with Remember this device turned off."); }
            }
            idToken = newId; refreshToken = newRefresh; UserId = uid;
            expiresAt = expiry; persistent = remember;
        }
        finally { gate.Release(); }
    }

    public async Task<bool> RestoreAsync(CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (refreshToken is not null) return true;
            string? saved;
            try { saved = await store.ReadAsync(); }
            catch
            {
                await ClearCoreAsync();
                throw new AuthException("STORAGE_FAILED", "The saved session could not be read. Sign in again.");
            }
            if (string.IsNullOrWhiteSpace(saved)) return false;
            refreshToken = saved; persistent = true;
            await RefreshCoreAsync(ct); // Always obtain a fresh token before restoring application access.
            return true;
        }
        finally { gate.Release(); }
    }

    public async Task<string> GetIdTokenAsync(CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (refreshToken is null)
                throw new AuthException("SIGN_IN_REQUIRED", "Sign in to continue.");
            if (idToken is null || expiresAt <= clock.GetUtcNow().AddMinutes(1))
                await RefreshCoreAsync(ct);
            return idToken!;
        }
        finally { gate.Release(); }
    }

    // Called only while gate is held; concurrent requests share one refresh operation.
    private async Task RefreshCoreAsync(CancellationToken ct)
    {
        var o = await OptionsAsync();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken! });
        using var response = await http.PostAsync(
            $"https://securetoken.googleapis.com/v1/token?key={Uri.EscapeDataString(o.ApiKey)}", content, ct);
        JsonDocument body;
        try { body = await ReadAsync(response, ct); }
        catch (AuthException error) when (error.Code is "TOKEN_EXPIRED" or "USER_DISABLED" or
            "USER_NOT_FOUND" or "INVALID_REFRESH_TOKEN" or "INVALID_GRANT")
        {
            await ClearCoreAsync(); SignedOut?.Invoke(); throw;
        }
        using (body)
        {
            var json = body.RootElement;
            var uid = Required(json, "user_id");
            if (UserId is not null && UserId != uid)
            {
                await ClearCoreAsync(); SignedOut?.Invoke();
                throw new AuthException("SESSION_MISMATCH", "Sign in again to continue.");
            }
            var newId = Required(json, "id_token");
            var newRefresh = Required(json, "refresh_token");
            var expiry = Expiry(Required(json, "expires_in"));
            if (persistent)
            {
                try { await store.WriteAsync(newRefresh); }
                catch
                {
                    await ClearCoreAsync(); SignedOut?.Invoke();
                    throw new AuthException("STORAGE_FAILED", "Your session could not be saved securely. Sign in again.");
                }
            }
            UserId = uid; idToken = newId; refreshToken = newRefresh; expiresAt = expiry;
        }
    }

    public async Task SendPasswordResetAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new AuthException("INPUT_REQUIRED", "Enter your email first.");
        var o = await OptionsAsync();
        using var response = await http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={Uri.EscapeDataString(o.ApiKey)}",
            new { requestType = "PASSWORD_RESET", email = email.Trim() }, ct);
        // Avoid revealing whether an account exists.
        try { using var ignored = await ReadAsync(response, ct); }
        catch (AuthException error) when (error.Code == "EMAIL_NOT_FOUND") { }
    }

    public async Task SignOutAsync()
    {
        await gate.WaitAsync();
        try { await ClearCoreAsync(); }
        finally { gate.Release(); SignedOut?.Invoke(); }
    }
    private async Task ClearCoreAsync()
    {
        idToken = null; refreshToken = null; UserId = null; persistent = false; expiresAt = default;
        try { await store.ClearAsync(); }
        catch { throw new AuthException("STORAGE_FAILED", "Memory session cleared, but saved sign-in could not be removed. Clear this app's data before sharing the device."); }
    }
    private DateTimeOffset Expiry(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
            throw new AuthException("INVALID_RESPONSE", "Firebase returned an invalid session expiry.");
        return clock.GetUtcNow().AddSeconds(seconds);
    }
    private static string Required(JsonElement json, string name)
    {
        if (!json.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new AuthException("INVALID_RESPONSE", "Firebase did not return a complete session. If your account requires MFA, use an MFA-capable sign-in flow.");
        return value.GetString()!;
    }
    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        JsonDocument body;
        try { body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); }
        catch (JsonException) { throw new AuthException("INVALID_RESPONSE", "Firebase returned an unexpected response. Try again later."); }
        if (response.IsSuccessStatusCode) return body;
        var code = "AUTH_FAILED";
        if (body.RootElement.TryGetProperty("error", out var error) &&
            error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.String)
            code = message.GetString()!.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? code;
        body.Dispose();
        throw new AuthException(code, code switch
        {
            "INVALID_LOGIN_CREDENTIALS" or "INVALID_PASSWORD" or "EMAIL_NOT_FOUND" => "Sign-in failed. Check your email and password.",
            "EMAIL_EXISTS" => "An account with this email already exists.",
            "INVALID_EMAIL" => "Enter a valid email address.",
            "WEAK_PASSWORD" or "PASSWORD_DOES_NOT_MEET_REQUIREMENTS" => "Your password does not meet the configured password policy.",
            "USER_DISABLED" => "This account is disabled. Contact your administrator.",
            "OPERATION_NOT_ALLOWED" or "PASSWORD_LOGIN_DISABLED" => "Email/password sign-in is not enabled for this Firebase project.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Wait and try again.",
            "INVALID_REFRESH_TOKEN" or "TOKEN_EXPIRED" or "USER_NOT_FOUND" or "INVALID_GRANT" => "Your session has expired. Sign in again.",
            "API_KEY_INVALID" or "PROJECT_NUMBER_MISMATCH" => "The Firebase project configuration needs attention.",
            _ => "Firebase could not complete the request. Try again or contact your administrator."
        });
    }
}
