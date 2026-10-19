using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface IAccountService
{
    Task<string> VerifyTokenAsync(string idToken);
    Task<ProfileResponse> GetProfileAsync();
    Task<ProfileResponse> CreateProfileAsync(CreateProfileRequest request);
    Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request);
}
