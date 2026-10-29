using FirebaseAdmin.Auth;
using Solvix.Web.Connected;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Web.Services;
public sealed class UserService : IUserService
{
    private readonly IUserRepository users;private readonly IFirebaseUserDirectory auth;private readonly ICurrentUserService user;
    private readonly CompanyService? companies;
    public UserService(IUserRepository users,IFirebaseUserDirectory auth,ICurrentUserService user,CompanyService? companies = null){this.users=users;this.auth=auth;this.user=user;this.companies=companies;}
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
        var saved = await users.UpdateAdminAsync(uid,r);
        if (saved.Status == "Active" && companies is not null) await companies.ApproveAccountCompanyAsync(actor.Uid, uid, r.CompanyRole);
        return saved;
    }
    public async Task DeleteAsync(string uid)
    {
        var actor = await user.RequireAsync(true);
        ServiceRules.Id(uid);
        if (uid == actor.Uid)
            throw new ServiceException(409, "You cannot delete your own administrator account.");
        if (await users.FindAsync(uid) is null) return; // Idempotent replay of a completed deletion.

        // Firebase Auth and Firestore do not share a transaction. Block application access first.
        await users.PrepareDeletionAsync(uid);
        await auth.DeleteAsync(uid);
        await users.DeleteAsync(uid);
    }
}
