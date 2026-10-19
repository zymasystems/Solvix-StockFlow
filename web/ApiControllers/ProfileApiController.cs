using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/profile")]
public sealed class ProfileApiController : ControllerBase
{
    private readonly IAccountService accounts;
    public ProfileApiController(IAccountService accounts) => this.accounts = accounts;
    [HttpGet]
    [Authorize(Policy = Policies.FirebaseIdentity)]
    public async Task<ActionResult<ProfileResponse>> Get() => Ok(await accounts.GetProfileAsync());
    [HttpPost]
    [Authorize(Policy = Policies.FirebaseIdentity)]
    public async Task<ActionResult<ProfileResponse>> Create([FromBody] CreateProfileRequest request)
    {
        var profile = await accounts.CreateProfileAsync(request);
        // Global profile status no longer grants inventory access; active company membership does.
        return StatusCode(201, profile);
    }
    [HttpPut]
    [Authorize(Policy = Policies.FirebaseIdentity)]
    public async Task<ActionResult<ProfileResponse>> Update([FromBody] UpdateProfileRequest request)
        => Ok(await accounts.UpdateProfileAsync(request));
}
