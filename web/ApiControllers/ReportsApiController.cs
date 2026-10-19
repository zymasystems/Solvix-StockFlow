using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/reports/inventory")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class ReportsApiController : ControllerBase
{
    private readonly IReportService service;
    public ReportsApiController(IReportService service) => this.service = service;
    [HttpGet]
    public async Task<ActionResult<InventoryReportResponse>> Get() => Ok(await service.GetInventoryAsync());
}
