using System.Text.Json;
using Microsoft.Maui.Storage;
namespace Solvix.Sims.Services;
public sealed class ApiSettings
{
    public string BaseUrl { get; }
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);
    public ApiSettings()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(stream);
        var address = doc.RootElement.GetProperty("Solvix").GetProperty("Api").GetProperty("BaseUrl").GetString();
        if (!Uri.TryCreate(address,UriKind.Absolute,out var uri) || uri.Scheme != "https")
            throw new InvalidOperationException("Configure Solvix.Api.BaseUrl with the HTTPS backend URL.");
        BaseUrl = address!.TrimEnd('/') + "/";
    }
}
