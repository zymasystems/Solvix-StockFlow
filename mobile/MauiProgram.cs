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
        builder.Services.AddSingleton<ApiSettings>();
        builder.Services.AddSingleton<ConnectionService>();
        builder.Services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
        builder.Services.AddSingleton<IFirebaseAuthService>(sp => new FirebaseAuthService(
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(30) },
            sp.GetRequiredService<IRefreshTokenStore>(), FirebaseAuthOptionsLoader.LoadAsync));
        builder.Services.AddSingleton<ApiService>(sp =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { BaseAddress = new Uri(settings.BaseUrl), Timeout = settings.Timeout };
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return new ApiService(http, sp.GetRequiredService<IFirebaseAuthService>(),
                sp.GetRequiredService<ConnectionService>());
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
