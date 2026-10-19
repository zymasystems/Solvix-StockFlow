using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;
public sealed class ReportService : IReportService
{
    private readonly IProductRepository productsRepo;private readonly ICurrentUserService user;
    public ReportService(IProductRepository productsRepo,ICurrentUserService user){this.productsRepo=productsRepo;this.user=user;}
    public async Task<InventoryReportResponse> GetInventoryAsync()
    {
        await user.RequireAsync();
        var products=await productsRepo.GetAllAsync();
        return new InventoryReportResponse{
            GeneratedAt=DateTimeOffset.UtcNow,Products=products,TotalProducts=products.Count,
            TotalUnits=products.Sum(x=>x.Stock),TotalInventoryValue=products.Sum(x=>x.Price*x.Stock),
            LowStockCount=products.Count(x=>x.Stock>0 && x.Stock<=x.MinStock),OutOfStockCount=products.Count(x=>x.Stock<=0)};
    }
}
