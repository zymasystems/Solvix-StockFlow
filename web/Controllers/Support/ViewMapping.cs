using Google.Cloud.Firestore;
using Solvix.Web.Models;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Controllers.Support;
internal static class ViewMapping
{
    private static Timestamp? Time(DateTimeOffset? value) => value.HasValue ? Timestamp.FromDateTime(value.Value.UtcDateTime) : null;
    public static Product Product(ProductResponse p) => new(){Id=p.Id,Name=p.Name,Sku=p.Sku,Category=p.Category,
        Price=(double)p.Price,Stock=p.Stock,MinStock=p.MinStock,Status=p.Status,CreatedAt=Time(p.CreatedAt)};
    public static Supplier Supplier(SupplierResponse p) => new(){Id=p.Id,Name=p.Name,ContactPerson=p.ContactPerson,
        Email=p.Email,Phone=p.Phone,ProductCount=p.ProductCount,Status=p.Status,Rating=p.Rating,CreatedAt=Time(p.CreatedAt)};
    public static Sale Sale(SaleResponse p) => new(){Id=p.Id,ProductName=p.ProductName,Type=p.Type,Quantity=p.Quantity,
        PerformedByName=p.PerformedByName,Notes=p.Notes??"",Date=p.LegacyDate??p.OccurredAt?.ToString("O")??"",Timestamp=Time(p.OccurredAt)};
    public static StockAdjustment Adjustment(StockAdjustmentResponse p) => new(){Id=p.Id,ProductName=p.ProductName,
        Type=p.Type,Change=p.Change,Reason=p.Reason,PerformedByName=p.PerformedByName,
        Date=p.LegacyDate??p.OccurredAt?.ToString("O")??"",Timestamp=Time(p.OccurredAt)};
    public static UserProfile User(ProfileResponse p) => new(){Id=p.Uid,Uid=p.Uid,FirstName=p.FirstName,LastName=p.LastName,
        DisplayName=p.DisplayName,Email=p.Email,Company=p.Company,Role=p.Role,Status=p.Status,PhotoUrl=p.PhotoUrl};
}
