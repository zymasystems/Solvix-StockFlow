using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Solvix.Web.Authentication;
using Solvix.Web.Repositories.Interfaces;

namespace Solvix.Web.Authorization;

public static class Policies
{
    public const string SmartScheme = "Solvix";
    public const string WebCookieScheme = "SolvixCookie";

    // Identity-only policy allows a verified new Firebase user to request a profile.
    public const string FirebaseIdentity = "FirebaseIdentity";
    public const string ApiActiveUser = "ApiActiveUser";
    public const string ApiAdmin = "ApiAdmin";
    public const string WebActiveUser = "WebActiveUser";
    public const string WebAdmin = "WebAdmin";
    public const string CompanyAdmin = "CompanyAdmin";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CompanyAdmin, p => p.AddAuthenticationSchemes(SmartScheme).RequireAuthenticatedUser().AddRequirements(new ActiveProfileRequirement(false), new CompanyAdminRequirement()));
        options.AddPolicy("CompanyIdentity", p => p.AddAuthenticationSchemes(SmartScheme).RequireAuthenticatedUser().AddRequirements(new ActiveProfileRequirement(false)));
        options.AddPolicy(FirebaseIdentity, policy => policy
            .AddAuthenticationSchemes(FirebaseBearerHandler.SchemeName)
            .RequireAuthenticatedUser());

        options.AddPolicy(ApiActiveUser, Build(FirebaseBearerHandler.SchemeName, false));
        options.AddPolicy(ApiAdmin, Build(FirebaseBearerHandler.SchemeName, true));
        options.AddPolicy(WebActiveUser, Build(WebCookieScheme, false));
        options.AddPolicy(WebAdmin, Build(WebCookieScheme, true));

        // Bare [Authorize] and endpoints without an attribute require an approved profile.
        // Mark intended public pages/actions explicitly [AllowAnonymous].
        options.DefaultPolicy = Build(SmartScheme, false);
        options.FallbackPolicy = options.DefaultPolicy;
    }

    private static AuthorizationPolicy Build(string scheme, bool admin)
        => new AuthorizationPolicyBuilder(scheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveProfileRequirement(admin))
            .Build();
}

internal sealed class ActiveProfileRequirement : IAuthorizationRequirement
{
    public bool AdminRequired { get; }
    public ActiveProfileRequirement(bool adminRequired) => AdminRequired = adminRequired;
}

// Uses the identity supplied by the policy, not a UID or role sent in request JSON.
internal sealed class ActiveProfileHandler : AuthorizationHandler<ActiveProfileRequirement>
{
    private readonly IUserRepository users;
    public ActiveProfileHandler(IUserRepository users) => this.users = users;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ActiveProfileRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(uid))
        {
            context.Fail();
            return;
        }

        var profile = await users.FindAsync(uid);
        if (profile is null || profile.Status != "Active" ||
            (profile.Role != "admin" && profile.Role != "staff") ||
            (requirement.AdminRequired && profile.Role != "admin"))
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}
