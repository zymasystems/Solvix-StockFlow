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

		// App state. Singletons so every page sees the same stock ledger.
		builder.Services.AddSingleton<SessionState>();
		builder.Services.AddSingleton<InventoryService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
