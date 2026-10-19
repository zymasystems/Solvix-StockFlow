using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Repositories.Interfaces;
public interface IInventoryRepository
{
    Task<SaleResponse> RecordSaleAsync(string productId,string type,long quantity,string notes,Guid requestId,ProfileResponse actor);
    Task<StockAdjustmentResponse> AdjustStockAsync(string productId,string type,long quantity,string reason,Guid requestId,ProfileResponse actor);
    Task<PagedResponse<SaleResponse>> ListSalesAsync(int size=50,string? cursor=null);
    Task<PagedResponse<StockAdjustmentResponse>> ListAdjustmentsAsync(int size=50,string? cursor=null);
    Task<List<SaleResponse>> GetAllSalesAsync();
    Task<List<StockAdjustmentResponse>> GetAllAdjustmentsAsync();
}
