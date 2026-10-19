using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Repositories.Internal;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Repositories;
public sealed class ProductRepository : IProductRepository
{
    private readonly FirestoreDb db;
    public ProductRepository(FirestoreDb db) { this.db=db; }
    public async Task<PagedResponse<ProductResponse>> ListAsync(int size=50,string? cursor=null)
    { return await FirestoreStore.Page(db,"products",size,cursor,FirestoreMapping.Product); }
    public async Task<List<ProductResponse>> GetAllAsync() => (await FirestoreStore.All(db,"products")).Select(FirestoreMapping.Product).ToList();
    public async Task<ProductResponse> GetAsync(string id)
    { return FirestoreMapping.Product(await FirestoreStore.Get(db,"products",id)); }
    public async Task<ProductResponse> CreateAsync(CreateProductRequest r, ProfileResponse actor)
    {
        var product=db.Collection("products").Document();
        var sku=r.Sku.Trim().ToUpperInvariant(); var key=db.Collection("skuReservations").Document(ServiceRules.Hash(sku));
        var opening=db.Collection("adjustments").Document();
        await db.RunTransactionAsync(async tx=>
        {
            var indexState=await tx.GetSnapshotAsync(db.Collection("system").Document("skuIndex"));
            if(!indexState.Exists || !indexState.TryGetValue<bool>("ready", out var ready) || !ready)
                throw new ServiceException(503,"The SKU index must be initialised before product writes.");
            if((await tx.GetSnapshotAsync(key)).Exists) throw new ServiceException(409,"SKU already exists.");
            tx.Create(key,new Dictionary<string,object>{{"productId",product.Id},{"sku",sku}});
            tx.Create(product,new Dictionary<string,object>{
                {"name",r.Name.Trim()},{"sku",sku},{"category",r.Category.Trim()},{"price",(double)r.Price},
                {"stock",r.OpeningStock},{"minStock",r.MinStock},{"status",ServiceRules.Status(r.OpeningStock,r.MinStock)},
                {"createdAt",FieldValue.ServerTimestamp}});
            tx.Create(opening,new Dictionary<string,object>{
                {"productId",product.Id},{"product",r.Name.Trim()},{"type","Set"},{"change","="+r.OpeningStock},
                {"previousStock",0},{"newStock",r.OpeningStock},{"reason","Opening stock"},
                {"by",actor.DisplayName},{"performedByUserId",actor.Uid},{"timestamp",FieldValue.ServerTimestamp}});
        });
        return FirestoreMapping.Product(await product.GetSnapshotAsync());
    }
    public async Task<ProductResponse> UpdateAsync(string id,UpdateProductRequest r)
    {
        
        var product=db.Collection("products").Document(ServiceRules.Id(id)); var sku=r.Sku.Trim().ToUpperInvariant();
        await db.RunTransactionAsync(async tx=>
        {
            var indexState=await tx.GetSnapshotAsync(db.Collection("system").Document("skuIndex"));
            if(!indexState.Exists || !indexState.TryGetValue<bool>("ready", out var ready) || !ready)
                throw new ServiceException(503,"The SKU index must be initialised before product writes.");
            var s=await tx.GetSnapshotAsync(product);
            if(!s.Exists) throw new ServiceException(404,"Product not found.");
            var oldSku=FirestoreMapping.Text(s,"sku").Trim().ToUpperInvariant();
            var oldKey=db.Collection("skuReservations").Document(ServiceRules.Hash(oldSku));
            var newKey=db.Collection("skuReservations").Document(ServiceRules.Hash(sku));
            var reservation=await tx.GetSnapshotAsync(newKey);
            var oldReservation=oldKey.Path==newKey.Path ? reservation : await tx.GetSnapshotAsync(oldKey);
            if(reservation.Exists && FirestoreMapping.Text(reservation,"productId")!=id)
                throw new ServiceException(409,"SKU already exists.");
            if(oldKey.Path!=newKey.Path && oldReservation.Exists && FirestoreMapping.Text(oldReservation,"productId")==id) tx.Delete(oldKey);
            tx.Set(newKey,new Dictionary<string,object>{{"productId",id},{"sku",sku}});
            tx.Update(product,new Dictionary<string,object>{{"name",r.Name.Trim()},{"sku",sku},{"category",r.Category.Trim()},
                {"price",(double)r.Price},{"minStock",r.MinStock},{"status",ServiceRules.Status(FirestoreMapping.Number(s,"stock"),r.MinStock)},
                {"updatedAt",FieldValue.ServerTimestamp}});
        });
        return FirestoreMapping.Product(await product.GetSnapshotAsync());
    }
    public async Task DeleteAsync(string id)
    {
        var product=db.Collection("products").Document(ServiceRules.Id(id));
        await db.RunTransactionAsync(async tx=>
        {
            var indexState=await tx.GetSnapshotAsync(db.Collection("system").Document("skuIndex"));
            if(!indexState.Exists || !indexState.TryGetValue<bool>("ready", out var ready) || !ready)
                throw new ServiceException(503,"The SKU index must be initialised before product writes.");
            var s=await tx.GetSnapshotAsync(product); if(!s.Exists) throw new ServiceException(404,"Product not found.");
            if(FirestoreMapping.Number(s,"stock")!=0) throw new ServiceException(409,"Adjust stock to zero before deleting a product.");
            var key=db.Collection("skuReservations").Document(ServiceRules.Hash(FirestoreMapping.Text(s,"sku").Trim().ToUpperInvariant()));
            var reservation=await tx.GetSnapshotAsync(key);
            if(reservation.Exists && FirestoreMapping.Text(reservation,"productId")==id) tx.Delete(key);
            tx.Delete(product); // History keeps the original product ID and name.
        });
    }
}
