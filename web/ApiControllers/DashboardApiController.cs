using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class DashboardApiController : ControllerBase
{
    private readonly IDashboardService service;
    public DashboardApiController(IDashboardService service) => this.service = service;
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get() => Ok(await service.GetAsync());
}
