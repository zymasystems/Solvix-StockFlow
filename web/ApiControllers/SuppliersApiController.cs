using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class SuppliersApiController : ControllerBase
{
    private readonly ISupplierService service;
    public SuppliersApiController(ISupplierService service) => this.service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierResponse>>> List([FromQuery] int pageSize = 50, [FromQuery] string? cursor = null)
        => Ok(await service.ListAsync(pageSize, cursor));

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierResponse>> Get(string id)
        => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Policy = Policies.ApiAdmin)]
    public async Task<ActionResult<SupplierResponse>> Create([FromBody] CreateSupplierRequest request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ApiAdmin)]
    public async Task<ActionResult<SupplierResponse>> Update(string id, [FromBody] UpdateSupplierRequest request)
        => Ok(await service.UpdateAsync(id, request));
}
