using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class DashboardService : IDashboardService
{
    private readonly IProductRepository productsRepo;private readonly ISupplierRepository suppliersRepo;private readonly IInventoryRepository inventory;private readonly ICurrentUserService user;
    public DashboardService(IProductRepository productsRepo,ISupplierRepository suppliersRepo,IInventoryRepository inventory,ICurrentUserService user){this.productsRepo=productsRepo;this.suppliersRepo=suppliersRepo;this.inventory=inventory;this.user=user;}
    public async Task<DashboardResponse> GetAsync()
    {
        await user.RequireAsync();
        var products=await productsRepo.GetAllAsync();
        var suppliers=await suppliersRepo.GetAllAsync();
        var sales=await inventory.GetAllSalesAsync();
        var adjustments=await inventory.GetAllAdjustmentsAsync();
        return new DashboardResponse{
            TotalProducts=products.Count,TotalSuppliers=suppliers.Count,TotalUnits=products.Sum(x=>x.Stock),
            InventoryValue=products.Sum(x=>x.Price*x.Stock),LowStockCount=products.Count(x=>x.Stock>0 && x.Stock<=x.MinStock),
            OutOfStockCount=products.Count(x=>x.Stock<=0),
            LowStockProducts=products.Where(x=>x.Stock<=x.MinStock).OrderBy(x=>x.Stock).Take(10).ToList(),
            // Unknown legacy dates are excluded rather than guessed or called recent.
            RecentSales=sales.Where(x=>x.OccurredAt.HasValue).OrderByDescending(x=>x.OccurredAt).Take(10).ToList(),
            RecentAdjustments=adjustments.Where(x=>x.OccurredAt.HasValue).OrderByDescending(x=>x.OccurredAt).Take(10).ToList()};
    }
}
