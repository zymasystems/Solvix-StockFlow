using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Repositories.Interfaces;
public interface IProductRepository
{
    Task<PagedResponse<ProductResponse>> ListAsync(int size=50,string? cursor=null);
    Task<List<ProductResponse>> GetAllAsync();
    Task<ProductResponse> GetAsync(string id);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, ProfileResponse actor);
    Task<ProductResponse> UpdateAsync(string id,UpdateProductRequest request);
    Task DeleteAsync(string id);
}
