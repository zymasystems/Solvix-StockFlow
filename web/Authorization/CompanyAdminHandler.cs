using Microsoft.AspNetCore.Authorization;
using Solvix.Web.Connected;
using Solvix.Web.Services;
namespace Solvix.Web.Authorization;
public sealed class CompanyAdminRequirement : IAuthorizationRequirement { }
public sealed class CompanyAdminHandler(CompanyMvcContext company) : AuthorizationHandler<CompanyAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CompanyAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        try { if ((await company.LoadAsync())?.Permission.Role == "admin") context.Succeed(requirement); }
        catch (ServiceException) { context.Fail(); }
    }
}
