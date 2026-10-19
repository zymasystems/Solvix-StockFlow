using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Repositories.Internal;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Repositories;
public sealed class SupplierRepository : ISupplierRepository
{
    private readonly FirestoreDb db;
    public SupplierRepository(FirestoreDb db){this.db=db;}
    public async Task<PagedResponse<SupplierResponse>> ListAsync(int size=50,string? cursor=null)
    {return await FirestoreStore.Page(db,"suppliers",size,cursor,FirestoreMapping.Supplier);}
    public async Task<List<SupplierResponse>> GetAllAsync() => (await FirestoreStore.All(db,"suppliers")).Select(FirestoreMapping.Supplier).ToList();
    public async Task<SupplierResponse> GetAsync(string id)
    {return FirestoreMapping.Supplier(await FirestoreStore.Get(db,"suppliers",id));}
    private static Dictionary<string,object> Fields(CreateSupplierRequest r)=> new(){
        {"name",r.Name.Trim()},{"contact",r.ContactPerson.Trim()},{"email",r.Email.Trim()},
        {"phone",r.Phone.Trim()},{"status",r.Status},{"rating",r.Rating}};
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest r)
    {
        var data=Fields(r);
        data["products"]=0; data["createdAt"]=FieldValue.ServerTimestamp;
        var doc=await db.Collection("suppliers").AddAsync(data);
        return FirestoreMapping.Supplier(await doc.GetSnapshotAsync());
    }
    public async Task<SupplierResponse> UpdateAsync(string id,UpdateSupplierRequest r)
    {
        
        var doc=db.Collection("suppliers").Document(ServiceRules.Id(id));
        await db.RunTransactionAsync(async tx=>{
            if(!(await tx.GetSnapshotAsync(doc)).Exists) throw new ServiceException(404,"Supplier not found.");
            tx.Update(doc,Fields(r));
        });
        return FirestoreMapping.Supplier(await doc.GetSnapshotAsync());
    }
    // Deactivate instead of deleting historical supplier information.
}
