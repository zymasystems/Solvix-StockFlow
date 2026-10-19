using System.Text.Json;
using Microsoft.Maui.Storage;
namespace Solvix.Sims.Services.Authentication;

public static class FirebaseAuthOptionsLoader
{
    public static async Task<FirebaseAuthOptions> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("authsettings.json");
        var result = await JsonSerializer.DeserializeAsync<FirebaseAuthOptions>(stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (result is null || string.IsNullOrWhiteSpace(result.ProjectId) ||
            string.IsNullOrWhiteSpace(result.ApiKey) || result.ApiKey.Contains("COPY_"))
            throw new InvalidOperationException("Configure the public Firebase ProjectId and ApiKey in authsettings.json.");
        return result;
    }
}
