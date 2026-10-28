using FirebaseAdmin.Auth;
using Solvix.Web.Services.Interfaces;
namespace Solvix.Web.Services;
public sealed class FirebaseUserDirectory(FirebaseAuth firebase) : IFirebaseUserDirectory
{
    public async Task VerifyExistsAsync(string uid) => await firebase.GetUserAsync(uid);
    public async Task DeleteAsync(string uid)
    {
        try { await firebase.DeleteUserAsync(uid); }
        catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // A retry may follow successful Firebase deletion but failed profile cleanup.
        }
        catch (FirebaseAuthException)
        {
            throw new ServiceException(503, "Firebase account deletion could not be completed. Access remains inactive. Retry deleting this user.");
        }
    }
}
