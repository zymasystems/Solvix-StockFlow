using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface ISupplierService
{
    Task<PagedResponse<SupplierResponse>> ListAsync(int size=50,string? cursor=null);
    Task<SupplierResponse> GetAsync(string id);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request);
    Task<SupplierResponse> UpdateAsync(string id,UpdateSupplierRequest request);
}
