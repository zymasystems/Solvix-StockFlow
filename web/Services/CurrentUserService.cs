using System.Security.Claims;
using Solvix.Web.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor http;
    private readonly IUserRepository users;
    public CurrentUserService(IHttpContextAccessor http, IUserRepository users) { this.http=http; this.users=users; }
    public string GetUid()
    {
        var user=http.HttpContext?.User;
        if(user?.Identity?.IsAuthenticated != true) throw new ServiceException(401,"Sign in first.");
        return ServiceRules.Id(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ServiceException(401,"Missing verified user ID."));
    }
    public async Task<ProfileResponse> RequireAsync(bool admin=false)
    {
        var p=await users.FindAsync(GetUid()) ?? throw new ServiceException(403,"An approved user profile is required.");
        if(p.Status!="Active" || (p.Role!="admin" && p.Role!="staff"))
            throw new ServiceException(403,"Your account is not approved for this application.");
        if(admin && p.Role!="admin") throw new ServiceException(403,"Administrator permission is required.");
        return p;
    }
}
