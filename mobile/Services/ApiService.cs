using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solvix.Sims.Services;

/// <summary>
/// JSON client for the Solvix web API.
/// - Stores and sends the Firebase ID token as <c>Authorization: Bearer &lt;token&gt;</c>
/// - Guards every call behind <see cref="ConnectionService"/>, throwing <see cref="ApiException"/> while offline
/// - Maps ProblemDetails responses to <see cref="ApiException"/> with the HTTP status and detail
/// Paths are relative to <see cref="ApiSettings.BaseUrl"/>, e.g. <c>api/products</c>.
/// </summary>
public sealed class ApiService
{
    private const string AuthTokenKey = "solvix.api.token";

    private readonly HttpClient _http;
    private readonly ApiSettings _settings;
    private readonly ConnectionService _connectivity;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, ApiSettings settings, ConnectionService connectivity)
    {
        _http = http;
        _settings = settings;
        _connectivity = connectivity;
    }

    /// <summary>Stores a Firebase ID token so authorized API calls send it as a bearer token.</summary>
    public Task SetAuthTokenAsync(string? idToken) =>
        string.IsNullOrWhiteSpace(idToken) ? Task.FromResult(SecureStorage.Remove(AuthTokenKey)) : SecureStorage.SetAsync(AuthTokenKey, idToken);

    /// <summary>Removes the stored bearer token.</summary>
    public Task ClearAuthTokenAsync() => Task.FromResult(SecureStorage.Remove(AuthTokenKey));

    private async Task<string?> GetAuthTokenAsync()
    {
        try { return await SecureStorage.GetAsync(AuthTokenKey); }
        catch { return null; }
    }

    private async Task EnsureOnline()
    {
        if (!_connectivity.IsConnected)
            throw new ApiException("No internet connection. Enable Wi-Fi or mobile data and try again.");
    }

    private async Task ApplyAuth()
    {
        var token = await GetAuthTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string?> ReadProblemDetail(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
        }
        catch { }
        return null;
    }

    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetail(response);
            throw new ApiException(
                $"The web API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                (int)response.StatusCode,
                detail);
        }

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    /// <summary>GET <paramref name="path"/> and deserialize the response as <typeparamref name="T"/>.</summary>
    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        await EnsureOnline();
        await ApplyAuth();
        using var response = await _http.GetAsync(path, cancellationToken);
        return await ReadJsonAsync<T>(response);
    }

    /// <summary>POST <paramref name="body"/> as JSON to <paramref name="path"/>, deserialize the response as <typeparamref name="TResult"/>.</summary>
    public async Task<TResult?> PostAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default)
    {
        await EnsureOnline();
        await ApplyAuth();
        using var content = JsonContent.Create(body, options: _json);
        using var response = await _http.PostAsync(path, content, cancellationToken);
        return await ReadJsonAsync<TResult>(response);
    }

    /// <summary>PUT <paramref name="body"/> as JSON to <paramref name="path"/>, deserialize the response as <typeparamref name="TResult"/>.</summary>
    public async Task<TResult?> PutAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default)
    {
        await EnsureOnline();
        await ApplyAuth();
        using var content = JsonContent.Create(body, options: _json);
        using var response = await _http.PutAsync(path, content, cancellationToken);
        return await ReadJsonAsync<TResult>(response);
    }

    /// <summary>DELETE <paramref name="path"/>. Throws <see cref="ApiException"/> on non-success.</summary>
    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        await EnsureOnline();
        await ApplyAuth();
        using var response = await _http.DeleteAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetail(response);
            throw new ApiException(
                $"The web API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                (int)response.StatusCode,
                detail);
        }
    }
}
