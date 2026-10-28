using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface IUserService
{
    Task<ProfileResponse> GetAsync(string uid);
    Task<PagedResponse<ProfileResponse>> ListAsync(int size=50,string? cursor=null);
    Task<ProfileResponse> UpdateAsync(string uid,Solvix.Web.ViewModels.Users.UserEditViewModel request);
    Task DeleteAsync(string uid);
}
