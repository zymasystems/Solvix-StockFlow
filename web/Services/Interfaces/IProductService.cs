using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface IProductService
{
    Task<PagedResponse<ProductResponse>> ListAsync(int size=50,string? cursor=null);
    Task<ProductResponse> GetAsync(string id);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(string id,UpdateProductRequest request);
    Task DeleteAsync(string id);
}
