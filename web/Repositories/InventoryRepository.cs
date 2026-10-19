using System.Globalization;
using System.Text.Json;
using Google.Cloud.Firestore;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Repositories.Internal;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Repositories;
// Internal implementation shared by sale and adjustment services.
public sealed class InventoryRepository : IInventoryRepository
{
    private readonly FirestoreDb db;
    public InventoryRepository(FirestoreDb db){this.db=db;}
    private async Task<DocumentSnapshot> WriteAsync(string collection,string productId,string type,long quantity,string text,Guid requestId,ProfileResponse actor)
    {
        ServiceRules.Id(productId);
        if(requestId==Guid.Empty) throw new ServiceException(400,"A nonempty request ID is required.");
        if(quantity<0 || (type!="Set" && quantity==0)) throw new ServiceException(400,"Invalid quantity.");
        var product=db.Collection("products").Document(productId);
        var operation=db.Collection("inventoryOperations").Document(ServiceRules.Hash(actor.Uid+":"+requestId.ToString("D")));
        var target=db.Collection(collection).Document();
        var fingerprint=ServiceRules.Hash(JsonSerializer.Serialize(new{collection,productId,type,quantity,text}));
        var resultId=await db.RunTransactionAsync(async tx=>
        {
            var existing=await tx.GetSnapshotAsync(operation);
            if(existing.Exists)
            {
                if(FirestoreMapping.Text(existing,"fingerprint")!=fingerprint)
                    throw new ServiceException(409,"This request ID was already used for a different operation.");
                return FirestoreMapping.Text(existing,"resultId");
            }
            var snap=await tx.GetSnapshotAsync(product);
            if(!snap.Exists) throw new ServiceException(404,"Product not found.");
            long before=FirestoreMapping.Number(snap,"stock"), after;
            if(before<0) throw new ServiceException(409,"Existing stock is invalid; reconcile it first.");
            try
            {
                after=type switch {
                    "Add" or "Return" => checked(before+quantity),
                    "Subtract" or "Sale" or "Transfer" => checked(before-quantity),
                    "Set" => quantity,
                    _ => throw new ServiceException(400,"Unsupported stock operation.")};
            }
            catch(OverflowException){throw new ServiceException(400,"Quantity is too large.");}
            if(after<0) throw new ServiceException(409,"Insufficient stock.");
            var data=new Dictionary<string,object>{
                {"productId",productId},{"product",FirestoreMapping.Text(snap,"name")},{"type",type},
                {"by",actor.DisplayName},{"performedByUserId",actor.Uid},{"timestamp",FieldValue.ServerTimestamp},
                {"previousStock",before},{"newStock",after},{"requestId",requestId.ToString("D")}};
            if(collection=="sales") {data["qty"]=quantity; data["notes"]=text;}
            else {
                data["reason"]=text;
                data["change"]=(type=="Set"?"=":type=="Add"?"+":"-")+quantity.ToString(CultureInfo.InvariantCulture);
            }
            tx.Update(product,new Dictionary<string,object>{{"stock",after},
                {"status",ServiceRules.Status(after,FirestoreMapping.Number(snap,"minStock"))},{"updatedAt",FieldValue.ServerTimestamp}});
            tx.Create(target,data);
            tx.Create(operation,new Dictionary<string,object>{{"fingerprint",fingerprint},{"resultId",target.Id},
                {"collection",collection},{"uid",actor.Uid},{"createdAt",FieldValue.ServerTimestamp}});
            return target.Id;
        });
        return await FirestoreStore.Get(db,collection,resultId);
    }

    public async Task<SaleResponse> RecordSaleAsync(string productId,string type,long quantity,string notes,Guid requestId,ProfileResponse actor)
        => FirestoreMapping.Sale(await WriteAsync("sales",productId,type,quantity,notes,requestId,actor));
    public async Task<StockAdjustmentResponse> AdjustStockAsync(string productId,string type,long quantity,string reason,Guid requestId,ProfileResponse actor)
        => FirestoreMapping.Adjustment(await WriteAsync("adjustments",productId,type,quantity,reason,requestId,actor));
    public Task<PagedResponse<SaleResponse>> ListSalesAsync(int size=50,string? cursor=null)
        => FirestoreStore.Page(db,"sales",size,cursor,FirestoreMapping.Sale);
    public Task<PagedResponse<StockAdjustmentResponse>> ListAdjustmentsAsync(int size=50,string? cursor=null)
        => FirestoreStore.Page(db,"adjustments",size,cursor,FirestoreMapping.Adjustment);
    public async Task<List<SaleResponse>> GetAllSalesAsync()
        => (await FirestoreStore.All(db,"sales")).Select(FirestoreMapping.Sale).ToList();
    public async Task<List<StockAdjustmentResponse>> GetAllAdjustmentsAsync()
        => (await FirestoreStore.All(db,"adjustments")).Select(FirestoreMapping.Adjustment).ToList();
}
