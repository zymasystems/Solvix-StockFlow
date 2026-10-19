namespace Solvix.Web.Services.Interfaces;
public interface IFirebaseUserDirectory
{
    Task VerifyExistsAsync(string uid);
}
