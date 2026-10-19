using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/sales")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class SalesApiController : ControllerBase
{
    private readonly ISalesService service;
    public SalesApiController(ISalesService service) => this.service = service;
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SaleResponse>>> List([FromQuery] int pageSize = 50, [FromQuery] string? cursor = null)
        => Ok(await service.ListAsync(pageSize, cursor));
    // 200 on first submission AND replay: service returns the same operation for a retry.
    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] RecordSaleRequest request)
        => Ok(await service.RecordAsync(request));
}
