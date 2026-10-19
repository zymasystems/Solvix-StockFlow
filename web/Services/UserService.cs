using FirebaseAdmin.Auth;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Web.Services;
public sealed class UserService : IUserService
{
    private readonly IUserRepository users;private readonly IFirebaseUserDirectory auth;private readonly ICurrentUserService user;
    public UserService(IUserRepository users,IFirebaseUserDirectory auth,ICurrentUserService user){this.users=users;this.auth=auth;this.user=user;}
    public async Task<ProfileResponse> GetAsync(string uid)
    {
        await user.RequireAsync(true);
        return await users.FindAsync(uid) ?? throw new ServiceException(404,"User profile not found.");
    }
    public async Task<PagedResponse<ProfileResponse>> ListAsync(int size=50,string? cursor=null)
    {await user.RequireAsync(true);return await users.ListAsync(size,cursor);}
    public async Task<ProfileResponse> UpdateAsync(string uid,UserEditViewModel r)
    {
        var actor=await user.RequireAsync(true);ServiceRules.Validate(r);ServiceRules.Id(uid);
        if(uid==actor.Uid && (r.Role!="admin" || r.Status!="Active"))
            throw new ServiceException(409,"You cannot remove your own administrator access.");
        await auth.VerifyExistsAsync(uid);
        return await users.UpdateAdminAsync(uid,r);
    }
}
