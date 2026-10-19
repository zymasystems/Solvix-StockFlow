using System.Net.Http.Headers;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Solvix.Sims.Services;

namespace Solvix.Sims;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		builder.Services.AddMauiBlazorWebView();

		// App state. Singletons so every page sees the same stock ledger and session.
		builder.Services.AddSingleton<SessionState>();
		builder.Services.AddSingleton<InventoryService>();

		// Web API connection layer. The app stays fully functional offline without these;
		// point mobile/appsettings.json Solvix.Api.BaseUrl at the web API to connect.
		builder.Services.AddSingleton<ApiSettings>();
		builder.Services.AddSingleton<ConnectionService>();
		builder.Services.AddHttpClient("Solvix.Api", (IServiceProvider sp, HttpClient client) =>
		{
			var settings = sp.GetRequiredService<ApiSettings>();
			client.BaseAddress = new Uri(settings.BaseUrl);
			client.Timeout = settings.Timeout;
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		});
		builder.Services.AddSingleton<ApiService>(sp =>
		{
			var factory = sp.GetRequiredService<IHttpClientFactory>();
			return new ApiService(
				factory.CreateClient("Solvix.Api"),
				sp.GetRequiredService<ApiSettings>(),
				sp.GetRequiredService<ConnectionService>());
		});

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
