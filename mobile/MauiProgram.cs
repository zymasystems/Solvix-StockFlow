using Microsoft.Extensions.Logging;
using Solvix.Sims.Services;
using Solvix.Sims.Services.Authentication;
using Solvix.Contracts;
namespace Solvix.Sims;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<SessionState>();
        builder.Services.AddScoped<InventoryService>();
        builder.Services.AddSingleton<ConnectionMessages>();
        builder.Services.AddSingleton<ConnectionService>();
        builder.Services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
        builder.Services.AddSingleton<IFirebaseAuthService>(sp => new FirebaseAuthService(
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(30) },
            sp.GetRequiredService<IRefreshTokenStore>(), FirebaseAuthOptionsLoader.LoadAsync));
        builder.Services.AddSingleton<ApiService>(sp =>
        {
            var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return new ApiService(http, sp.GetRequiredService<IFirebaseAuthService>(),
                sp.GetRequiredService<ConnectionService>(), async ct =>
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                        return await ApiSettings.LoadAsync(stream, ct);
                    }
                    catch (IOException)
                    {
                        throw new ApiException("The packaged appsettings.json could not be read. Check its MauiAsset entry and rebuild the app.");
                    }
                });
        });
        builder.Services.AddSingleton<MobileAuthCoordinator>();
        builder.Services.AddScoped<IWorkspaceClient, MobileWorkspaceClient>();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
