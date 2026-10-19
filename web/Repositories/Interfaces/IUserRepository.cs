using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Repositories.Interfaces;
public interface IUserRepository
{
    Task<ProfileResponse?> FindAsync(string uid);
    Task<PagedResponse<ProfileResponse>> ListAsync(int size=50,string? cursor=null);
    Task<ProfileResponse> CreateAsync(string uid,string verifiedEmail,CreateProfileRequest request);
    Task<ProfileResponse> UpdateNamesAsync(string uid,string firstName,string lastName);
    Task<ProfileResponse> UpdateAdminAsync(string uid,Solvix.Web.ViewModels.Users.UserEditViewModel request);
}
