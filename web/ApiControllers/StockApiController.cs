using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/stock-adjustments")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class StockApiController : ControllerBase
{
    private readonly IStockService service;
    public StockApiController(IStockService service) => this.service = service;
    [HttpGet]
    public async Task<ActionResult<PagedResponse<StockAdjustmentResponse>>> List([FromQuery] int pageSize = 50, [FromQuery] string? cursor = null)
        => Ok(await service.ListAsync(pageSize, cursor));
    // 200 on first submission AND replay: service returns the same operation for a retry.
    [HttpPost]
    public async Task<ActionResult<StockAdjustmentResponse>> Create([FromBody] AdjustStockRequest request)
        => Ok(await service.AdjustAsync(request));
}
