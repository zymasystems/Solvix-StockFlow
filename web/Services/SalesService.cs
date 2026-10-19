using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class SalesService : ISalesService
{
    private readonly IInventoryRepository inventory;private readonly ICurrentUserService user;
    public SalesService(IInventoryRepository inventory,ICurrentUserService user){this.inventory=inventory;this.user=user;}
    public async Task<SaleResponse> RecordAsync(RecordSaleRequest r)
    {
        var actor=await user.RequireAsync();ServiceRules.Validate(r);
        return await inventory.RecordSaleAsync(r.ProductId,r.Type,r.Quantity,r.Notes?.Trim()??"",r.RequestId!.Value,actor);
    }
    public async Task<PagedResponse<SaleResponse>> ListAsync(int size=50,string? cursor=null)
    {await user.RequireAsync();return await inventory.ListSalesAsync(size,cursor);}
}
