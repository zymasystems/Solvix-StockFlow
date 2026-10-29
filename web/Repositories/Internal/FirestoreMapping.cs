using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Repositories.Internal;
internal static class FirestoreMapping
{
    public static string Text(DocumentSnapshot s, string field) => s.TryGetValue<string>(field, out var v) ? v ?? "" : "";
    public static long Number(DocumentSnapshot s, string field) => s.TryGetValue<long>(field, out var v) ? v : 0;
    public static double Real(DocumentSnapshot s, string field) => s.TryGetValue<double>(field, out var v) ? v : 0;
    public static DateTimeOffset? Time(
     DocumentSnapshot snapshot,
     string field)
    {
        if (!snapshot.TryGetValue<object>(field, out var value)
            || value is null)
        {
            return null;
        }

        if (value is Timestamp timestamp)
        {
            return new DateTimeOffset(timestamp.ToDateTime());
        }

        // Leave legacy dates unknown until their format is confirmed.
        return null;
    }
    public static Guid? RequestId(DocumentSnapshot s) => Guid.TryParse(Text(s,"requestId"), out var v) ? v : null;
    public static ProductResponse Product(DocumentSnapshot s) => new()
    {
        Id=s.Id, Code=Text(s,"code"), Name=Text(s,"name"), Sku=Text(s,"sku"), Category=Text(s,"category"),
        Price=ServiceRules.Money(Real(s,"price")), Stock=Number(s,"stock"), MinStock=Number(s,"minStock"),
        Status=ServiceRules.Status(Number(s,"stock"),Number(s,"minStock")), CreatedAt=Time(s,"createdAt")
    };
    public static SupplierResponse Supplier(DocumentSnapshot s) => new()
    {
        Id=s.Id, Code=Text(s,"code"), Name=Text(s,"name"), ContactPerson=Text(s,"contact"), Email=Text(s,"email"), Phone=Text(s,"phone"),
        ProductCount=Number(s,"products"), Status=Text(s,"status"), Rating=Real(s,"rating"), CreatedAt=Time(s,"createdAt")
    };
    public static SaleResponse Sale(DocumentSnapshot s) => new()
    {
        Id=s.Id, Code=Text(s,"code"), ProductId=NullText(s,"productId"), ProductName=Text(s,"product"), Type=Text(s,"type"),
        Quantity=Number(s,"qty"), PerformedByUserId=NullText(s,"performedByUserId"), PerformedByName=Text(s,"by"),
        Notes=NullText(s,"notes"), OccurredAt=Time(s,"timestamp"), LegacyDate=NullText(s,"date"), RequestId=RequestId(s)
    };
    public static StockAdjustmentResponse Adjustment(DocumentSnapshot s) => new()
    {
        Id=s.Id, Code=Text(s,"code"), ProductId=NullText(s,"productId"), ProductName=Text(s,"product"), Type=Text(s,"type"),
        Change=Text(s,"change"), Reason=Text(s,"reason"), PerformedByUserId=NullText(s,"performedByUserId"),
        PerformedByName=Text(s,"by"), OccurredAt=Time(s,"timestamp"), LegacyDate=NullText(s,"date"),
        PreviousStock=s.ContainsField("previousStock") ? Number(s,"previousStock") : null,
        NewStock=s.ContainsField("newStock") ? Number(s,"newStock") : null, RequestId=RequestId(s)
    };
    public static ProfileResponse Profile(DocumentSnapshot s) => new()
    {
        CreatedAt=Time(s,"createdAt"), Uid=s.Id, Code=Text(s,"code"), FirstName=Text(s,"firstName"), LastName=Text(s,"lastName"),
        DisplayName=Name(s), Email=Text(s,"email"), Company=Text(s,"company"),
        Role=Text(s,"role"), Status=Text(s,"status"), PhotoUrl=NullText(s,"photoURL")
    };
    private static string? NullText(DocumentSnapshot s,string f) => string.IsNullOrEmpty(Text(s,f)) ? null : Text(s,f);
    private static string Name(DocumentSnapshot s)
    {
        foreach(var f in new[]{"fullName","displayName"}) if(!string.IsNullOrWhiteSpace(Text(s,f))) return Text(s,f);
        var name=$"{Text(s,"firstName")} {Text(s,"lastName")}".Trim();
        return name.Length>0 ? name : Text(s,"email");
    }
}
