using Solvix.Web.Connected;
using Solvix.Contracts;
using Solvix.Web.Extensions;
using Solvix.Web.Middleware;
using Solvix.Web.Repositories;
using Solvix.Web.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Razor views for the website and attribute-routed JSON API controllers.
builder.Services.AddControllersWithViews().ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new RetiredControllers()));
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICompanyStore, FirestoreCompanyStore>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<IConnectedProfiles, ConnectedProfiles>();
builder.Services.AddScoped<IWorkspaceClient, ServerWorkspaceClient>();

// Includes Firestore, Firebase Admin, all repositories and application services.
// Use the repository package's current ServiceCollectionExtensions.
builder.Services.AddSolvixServices(builder.Configuration);

// Original MVC screens use company-scoped adapters; Firebase account services stay separate.
builder.Services.AddScoped<CompanyMvcContext>();
builder.Services.AddScoped<CompanyPageFilter>();
builder.Services.AddScoped<CompanyMvcServices>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Solvix.Web.Authorization.CompanyAdminHandler>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.IProductService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.ISupplierService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.IDashboardService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.IReportService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.IStockService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.ISalesService, CompanyMvcServices>();
builder.Services.AddScoped<Solvix.Web.Services.Interfaces.IUserService, CompanyMvcServices>();

// Website cookie authentication and Firebase bearer authentication for /api.
builder.Services.AddSolvixAuthentication(builder.Configuration);

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    foreach (var address in builder.Configuration.GetSection("Hosting:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
        options.KnownProxies.Add(System.Net.IPAddress.Parse(address));
});
var app = builder.Build();
app.UseForwardedHeaders();

// Consistent ProblemDetails responses; does not reveal provider exception details.
app.UseMiddleware<SolvixExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Only public CSS, JavaScript and images belong in wwwroot.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub().RequireAuthorization("CompanyIdentity");
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
