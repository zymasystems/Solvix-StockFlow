using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class SupplierService : ISupplierService
{
    private readonly ISupplierRepository suppliers;private readonly ICurrentUserService user;
    public SupplierService(ISupplierRepository suppliers,ICurrentUserService user){this.suppliers=suppliers;this.user=user;}
    public async Task<PagedResponse<SupplierResponse>> ListAsync(int size=50,string? cursor=null)
    {await user.RequireAsync();return await suppliers.ListAsync(size,cursor);}
    public async Task<SupplierResponse> GetAsync(string id)
    {await user.RequireAsync();return await suppliers.GetAsync(id);}
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest r)
    {await user.RequireAsync(true);ServiceRules.Validate(r);return await suppliers.CreateAsync(r);}
    public async Task<SupplierResponse> UpdateAsync(string id,UpdateSupplierRequest r)
    {await user.RequireAsync(true);ServiceRules.Validate(r);return await suppliers.UpdateAsync(id,r);}
}
