using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Solvix.Sims.Services.Authentication;

namespace Solvix.Sims.Services;
/// <summary>Authenticated JSON API client. All calls obtain a current Firebase token.</summary>
public sealed class ApiService
{
    private readonly HttpClient http;
    private readonly IFirebaseAuthService auth;
    private readonly IConnectionStatus connectivity;
    private readonly Func<CancellationToken, Task<Uri>>? loadBaseAddress;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public ApiService(HttpClient http, IFirebaseAuthService auth, IConnectionStatus connectivity,
        Func<CancellationToken, Task<Uri>>? loadBaseAddress = null)
    {
        this.http = http;
        this.auth = auth;
        this.connectivity = connectivity;
        this.loadBaseAddress = loadBaseAddress;
        if (loadBaseAddress is null && (http.BaseAddress is null || http.BaseAddress.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Set Solvix.Api.BaseUrl to your reachable HTTPS web backend URL.");
    }

    public Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default) => SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);
    public Task<TResult?> PostAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default) => SendAsync<TResult>(HttpMethod.Post, path, JsonContent.Create(body, options: Json), cancellationToken);
    public Task<TResult?> PutAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default) => SendAsync<TResult>(HttpMethod.Put, path, JsonContent.Create(body, options: Json), cancellationToken);
    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default) => await SendAsync<object>(HttpMethod.Delete, path, null, cancellationToken);
    private async Task<T?> SendAsync<T>(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var contentScope = content;
        var baseAddress = loadBaseAddress is null ? http.BaseAddress! : await loadBaseAddress(ct);
        if (baseAddress is null || !baseAddress.IsAbsoluteUri || baseAddress.Scheme != Uri.UriSchemeHttps)
            throw new ApiException("Set Solvix.Api.BaseUrl to your reachable HTTPS web backend URL.");
        var target = CheckedUri(baseAddress, path);
        if (!connectivity.IsConnected)
            throw new ApiException("No internet connection. Try again when connected.");
        var token = await auth.GetIdTokenAsync(ct);
        HttpResponseMessage? response = null;
        for (var attempt = 0;; attempt++)
        {
            using var request = new HttpRequestMessage(method, target)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                response = await http.SendAsync(request, ct);
            }
            catch (HttpRequestException)when (method == HttpMethod.Get && attempt < 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * (1 << attempt)), ct);
                continue;
            }
            catch (HttpRequestException)
            {
                throw new ApiException("Connection failed. Retry the same unchanged form for an uncertain submission.");
            }

            if (method == HttpMethod.Get && attempt < 2 && (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or HttpStatusCode.TooManyRequests))
            {
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(200 * (1 << attempt));
                response.Dispose();
                await Task.Delay(delay > TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : delay < TimeSpan.Zero ? TimeSpan.Zero : delay, ct);
                continue;
            }

            break;
        }

        using var responseScope = response;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await auth.SignOutAsync();
            throw new AuthException("SIGN_IN_REQUIRED", "Your session is no longer accepted. Sign in again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = "The server could not complete this request.";
            try
            {
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (error.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                    detail = d.GetString() ?? detail;
                else if (error.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    detail = string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.ValueKind == JsonValueKind.Array ? x.Value.EnumerateArray().Select(v => v.GetString() ?? "") : Array.Empty<string>()));
            }
            catch (JsonException)
            {
            }

            throw new ApiException("The web API rejected the request.", (int)response.StatusCode, detail);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;
        var json = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (JsonException)
        {
            throw new ApiException("The web API returned an unexpected response.");
        }
    }

    private static Uri CheckedUri(Uri baseAddress, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("api/", StringComparison.Ordinal) || path.Contains('\\') || path.Contains('#'))
            throw new ArgumentException("Use a relative api/... path.", nameof(path));
        var uri = new Uri(baseAddress, path);
        var prefix = new Uri(baseAddress, "api/");
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Authority != prefix.Authority || !uri.AbsolutePath.StartsWith(prefix.AbsolutePath, StringComparison.Ordinal))
            throw new ArgumentException("The API path must remain within the configured backend.", nameof(path));
        return uri;
    }
}
