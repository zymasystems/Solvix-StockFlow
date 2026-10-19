using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface ICurrentUserService
{
    string GetUid();
    Task<ProfileResponse> RequireAsync(bool admin=false);
}
