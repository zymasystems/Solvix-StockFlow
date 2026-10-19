using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.ApiControllers;
[ApiController]
[Route("api/products")]
[Authorize(Policy = Policies.ApiActiveUser)]
public sealed class ProductsApiController : ControllerBase
{
    private readonly IProductService service;
    public ProductsApiController(IProductService service) => this.service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> List([FromQuery] int pageSize = 50, [FromQuery] string? cursor = null)
        => Ok(await service.ListAsync(pageSize, cursor));

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> Get(string id)
        => Ok(await service.GetAsync(id));

    [HttpPost]
    
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    
    public async Task<ActionResult<ProductResponse>> Update(string id, [FromBody] UpdateProductRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ApiAdmin)]
    public async Task<IActionResult> Delete(string id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
