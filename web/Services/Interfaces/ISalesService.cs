using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface ISalesService
{
    Task<SaleResponse> RecordAsync(RecordSaleRequest request);
    Task<PagedResponse<SaleResponse>> ListAsync(int size=50,string? cursor=null);
}
