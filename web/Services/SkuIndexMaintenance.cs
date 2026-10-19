using Google.Cloud.Firestore;
using Solvix.Web.Repositories.Internal;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Services;

// Invoke from a trusted one-off maintenance process, never an HTTP endpoint.
// Stop ALL writers (including the legacy PHP app) before running.
public static class SkuIndexMaintenance
{
    public static async Task InitialiseAsync(FirestoreDb db)
    {
        var state=db.Collection("system").Document("skuIndex");
        await state.SetAsync(new Dictionary<string,object>{{"ready",false}});
        var products=await FirestoreStore.All(db,"products");
        var entries=products.Select(p=>new{Product=p.Id,Sku=FirestoreMapping.Text(p,"sku").Trim().ToUpperInvariant()}).ToList();
        if(entries.Any(x=>string.IsNullOrWhiteSpace(x.Sku)))
            throw new InvalidOperationException("Resolve empty SKUs before initialising the index.");
        if(entries.GroupBy(x=>x.Sku).Any(g=>g.Count()>1))
            throw new InvalidOperationException("Resolve duplicate SKUs before initialising the index.");
        // Intended for first migration. Existing conflicting reservations fail safely.
        foreach(var entry in entries)
        {
            var existing=await db.Collection("skuReservations").Document(ServiceRules.Hash(entry.Sku)).GetSnapshotAsync();
            if(existing.Exists && FirestoreMapping.Text(existing,"productId")!=entry.Product)
                throw new InvalidOperationException("A conflicting SKU reservation requires manual reconciliation.");
        }
        foreach(var group in entries.Chunk(400))
        {
            var batch=db.StartBatch();
            foreach(var entry in group)
                batch.Set(db.Collection("skuReservations").Document(ServiceRules.Hash(entry.Sku)),
                    new Dictionary<string,object>{{"productId",entry.Product},{"sku",entry.Sku}});
            await batch.CommitAsync();
        }
        await state.SetAsync(new Dictionary<string,object>{{"ready",true},{"initialisedAt",FieldValue.ServerTimestamp}});
    }
}
