using FirebaseAdmin.Auth;
using Solvix.Web.Authentication;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class AccountService : IAccountService
{
    private readonly FirebaseAuth auth;private readonly IUserRepository users;private readonly ICurrentUserService user;
    private readonly FirebaseTokenVerifier verifier;
    public AccountService(FirebaseAuth auth,IUserRepository users,ICurrentUserService user,FirebaseTokenVerifier verifier){this.auth=auth;this.users=users;this.user=user;this.verifier=verifier;}
    public async Task<string> VerifyTokenAsync(string idToken)
    {
        if(string.IsNullOrWhiteSpace(idToken)) throw new ServiceException(401,"An ID token is required.");
        try{return (await verifier.VerifyAsync(idToken)).Uid;}
        catch(FirebaseAuthException){throw new ServiceException(401,"Invalid, expired, revoked, or disabled identity. Sign in again.");}
    }
    public async Task<ProfileResponse> GetProfileAsync()
    {
        var uid = user.GetUid();
        var profile = await users.FindAsync(uid);
        if (profile is not null) return profile;
        var identity = await auth.GetUserAsync(uid);
        return new ProfileResponse { Uid = uid, Email = identity.Email ?? "", Role = "staff", Status = "Inactive" };
    }
    public async Task<ProfileResponse> CreateProfileAsync(CreateProfileRequest r)
    {
        ServiceRules.Validate(r);var uid=user.GetUid();var identity=await auth.GetUserAsync(uid);
        if(identity.Disabled) throw new ServiceException(403,"This identity is disabled.");
        return await users.CreateAsync(uid,identity.Email??"",r);
    }
    public async Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest r)
    {
        var uid=user.GetUid();ServiceRules.Validate(r);
        return await users.UpdateNamesAsync(uid,r.FirstName,r.LastName);
    }
}
