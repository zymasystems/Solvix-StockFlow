using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Repositories.Internal;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Web.Repositories;
public sealed class UserRepository : IUserRepository
{
    private readonly FirestoreDb db;
    public UserRepository(FirestoreDb db){this.db=db;}
    public async Task<ProfileResponse?> FindAsync(string uid)
    {
        var doc=await db.Collection("users").Document(ServiceRules.Id(uid)).GetSnapshotAsync();
        return doc.Exists ? FirestoreMapping.Profile(doc) : null;
    }
    public Task<PagedResponse<ProfileResponse>> ListAsync(int size=50,string? cursor=null)
        => FirestoreStore.Page(db,"users",size,cursor,FirestoreMapping.Profile);
    public async Task<ProfileResponse> CreateAsync(string uid,string verifiedEmail,CreateProfileRequest r)
    {
        var doc=db.Collection("users").Document(ServiceRules.Id(uid));
        await db.RunTransactionAsync(async tx=>{
            if((await tx.GetSnapshotAsync(doc)).Exists) throw new ServiceException(409,"Profile already exists.");
            tx.Create(doc,new Dictionary<string,object>{
                {"uid",uid},{"email",verifiedEmail},{"firstName",r.FirstName.Trim()},{"lastName",r.LastName.Trim()},
                {"fullName",$"{r.FirstName.Trim()} {r.LastName.Trim()}"},{"company",r.Company.Trim()},
                {"role","staff"},{"status","Inactive"},{"createdAt",FieldValue.ServerTimestamp},
                {"termsAcceptedAt",FieldValue.ServerTimestamp}});
        });
        return FirestoreMapping.Profile(await doc.GetSnapshotAsync());
    }
    public async Task<ProfileResponse> UpdateNamesAsync(string uid,string firstName,string lastName)
    {
        var fields=new Dictionary<string,object>{{"firstName",firstName.Trim()},{"lastName",lastName.Trim()},
            {"fullName",$"{firstName.Trim()} {lastName.Trim()}"},{"updatedAt",FieldValue.ServerTimestamp}};
        return await UpdateAsync(uid,fields);
    }
    public Task<ProfileResponse> UpdateAdminAsync(string uid,UserEditViewModel r)
        => UpdateAsync(uid,new Dictionary<string,object>{{"firstName",r.FirstName.Trim()},{"lastName",r.LastName.Trim()},
            {"fullName",$"{r.FirstName.Trim()} {r.LastName.Trim()}"},{"company",r.Company?.Trim()??""},
            {"role",r.Role},{"status",r.Status},{"updatedAt",FieldValue.ServerTimestamp}});
    private async Task<ProfileResponse> UpdateAsync(string uid,Dictionary<string,object> fields)
    {
        var doc=db.Collection("users").Document(ServiceRules.Id(uid));
        await db.RunTransactionAsync(async tx=>{
            if(!(await tx.GetSnapshotAsync(doc)).Exists) throw new ServiceException(404,"User profile not found.");
            tx.Update(doc,fields);
        });
        return FirestoreMapping.Profile(await doc.GetSnapshotAsync());
    }
}
