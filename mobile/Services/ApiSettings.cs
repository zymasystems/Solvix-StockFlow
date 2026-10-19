using System.Text.Json;

namespace Solvix.Sims.Services;

/// <summary>Reads packaged settings asynchronously when a backend call is needed.</summary>
public static class ApiSettings
{
    public static async Task<Uri> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        const string message = "Set Solvix.Api.BaseUrl in mobile/appsettings.json to your reachable HTTPS backend URL, then rebuild the app.";
        try
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("Solvix", out var solvix) || solvix.ValueKind != JsonValueKind.Object ||
                !solvix.TryGetProperty("Api", out var api) || api.ValueKind != JsonValueKind.Object ||
                !api.TryGetProperty("BaseUrl", out var value) || value.ValueKind != JsonValueKind.String)
                throw new ApiException(message);
            var address = value.GetString()?.Trim();
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                uri.Host.Contains("YOUR_API_HOST", StringComparison.OrdinalIgnoreCase))
                throw new ApiException(message);
            return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
        }
        catch (JsonException)
        {
            throw new ApiException("mobile/appsettings.json is not valid JSON. Correct it and rebuild the app.");
        }
    }
}
