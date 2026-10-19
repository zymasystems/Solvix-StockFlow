using FirebaseAdmin.Auth;
using Solvix.Web.Services.Interfaces;
namespace Solvix.Web.Services;
public sealed class FirebaseUserDirectory(FirebaseAuth firebase) : IFirebaseUserDirectory
{
    public async Task VerifyExistsAsync(string uid) => await firebase.GetUserAsync(uid);
}
