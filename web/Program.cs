using Solvix.Web.Extensions;
using Solvix.Web.Middleware;
using Solvix.Web.Repositories;
using Solvix.Web.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Razor views for the website and attribute-routed JSON API controllers.
builder.Services.AddControllersWithViews();

// Includes Firestore, Firebase Admin, all repositories and application services.
// Use the repository package's current ServiceCollectionExtensions.
builder.Services.AddSolvixServices(builder.Configuration);

// Website cookie authentication and Firebase bearer authentication for /api.
builder.Services.AddSolvixAuthentication(builder.Configuration);

var app = builder.Build();

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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
