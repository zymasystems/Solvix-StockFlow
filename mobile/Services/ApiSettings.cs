using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Solvix.Sims.Services;

/// <summary>
/// Connection settings for the Solvix web API.
/// The mobile app works offline by default (app-local ledger in <see cref="InventoryService"/>).
/// Point <see cref="BaseUrl"/> at the web API host to enable web connections.
/// </summary>
public sealed class ApiSettings
{
    private const string DefaultDebugBaseUrl = "http://10.0.2.2:5118/"; // Android emulator reaches the host machine on 10.0.2.2
    private const string DefaultReleaseBaseUrl = "https://api.solvix.example.com/";

    /// <summary>Base address of the Solvix web API, including trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>Timeout for every web API call.</summary>
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);

    public ApiSettings()
    {
        BaseUrl = LoadBaseUrl();
    }

    private static string LoadBaseUrl()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Solvix", out var solvix)
                && solvix.TryGetProperty("Api", out var api)
                && api.TryGetProperty("BaseUrl", out var url)
                && url.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(url.GetString()))
            {
                return EnsureTrailingSlash(url.GetString()!);
            }
        }
        catch
        {
            // appsettings.json missing or unreadable on this device -> fall back to the defaults below
        }

#if DEBUG
        return DefaultDebugBaseUrl;
#else
        return DefaultReleaseBaseUrl;
#endif
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
}
