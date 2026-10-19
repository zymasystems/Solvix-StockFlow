using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solvix.Web.Authentication;
using Solvix.Web.Authorization;
using Solvix.Web.Configuration;

namespace Solvix.Web.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddSolvixAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProjectId),
                "Firebase:ProjectId is required.")
            .ValidateOnStart();

        // FirebaseAuth itself is registered once by AddSolvixServices.
        services.AddScoped<FirebaseTokenVerifier>();
        services.AddScoped<IAuthorizationHandler, ActiveProfileHandler>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Policies.SmartScheme;
            options.DefaultChallengeScheme = Policies.SmartScheme;
            options.DefaultForbidScheme = Policies.SmartScheme;
            options.DefaultSignInScheme = Policies.WebCookieScheme;
            options.DefaultSignOutScheme = Policies.WebCookieScheme;
        })
        .AddPolicyScheme(Policies.SmartScheme, "Website or Firebase API", options =>
        {
            // API routes never fall back to a browser cookie.
            options.ForwardDefaultSelector = context =>
                context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                    ? FirebaseBearerHandler.SchemeName
                    : Policies.WebCookieScheme;
        })
        .AddCookie(Policies.WebCookieScheme, options =>
        {
            options.Cookie.Name = "__Host-Solvix.Auth";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.SlidingExpiration = false;
        })
        .AddScheme<AuthenticationSchemeOptions, FirebaseBearerHandler>(
            FirebaseBearerHandler.SchemeName, _ => { });

        services.AddAuthorization(Policies.Configure);
        return services;
    }
}
