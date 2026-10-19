using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class ProductService : IProductService
{
    private readonly IProductRepository products;private readonly ICurrentUserService user;
    public ProductService(IProductRepository products,ICurrentUserService user){this.products=products;this.user=user;}
    public async Task<PagedResponse<ProductResponse>> ListAsync(int size=50,string? cursor=null)
    {await user.RequireAsync();return await products.ListAsync(size,cursor);}
    public async Task<ProductResponse> GetAsync(string id)
    {await user.RequireAsync();return await products.GetAsync(id);}
    public async Task<ProductResponse> CreateAsync(CreateProductRequest r)
    {
        var actor=await user.RequireAsync();ServiceRules.Validate(r);ServiceRules.Price(r.Price);
        return await products.CreateAsync(r,actor);
    }
    public async Task<ProductResponse> UpdateAsync(string id,UpdateProductRequest r)
    {
        await user.RequireAsync();ServiceRules.Validate(r);ServiceRules.Price(r.Price);
        return await products.UpdateAsync(id,r);
    }
    public async Task DeleteAsync(string id){await user.RequireAsync(true);await products.DeleteAsync(id);}
}
