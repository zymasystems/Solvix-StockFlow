using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface IStockService
{
    Task<StockAdjustmentResponse> AdjustAsync(AdjustStockRequest request);
    Task<PagedResponse<StockAdjustmentResponse>> ListAsync(int size=50,string? cursor=null);
}
