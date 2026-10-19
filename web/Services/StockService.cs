using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class StockService : IStockService
{
    private readonly IInventoryRepository inventory;private readonly ICurrentUserService user;
    public StockService(IInventoryRepository inventory,ICurrentUserService user){this.inventory=inventory;this.user=user;}
    public async Task<StockAdjustmentResponse> AdjustAsync(AdjustStockRequest r)
    {
        var actor=await user.RequireAsync();ServiceRules.Validate(r);
        return await inventory.AdjustStockAsync(r.ProductId,r.Type,r.Quantity!.Value,r.Reason.Trim(),r.RequestId!.Value,actor);
    }
    public async Task<PagedResponse<StockAdjustmentResponse>> ListAsync(int size=50,string? cursor=null)
    {await user.RequireAsync();return await inventory.ListAdjustmentsAsync(size,cursor);}
}
