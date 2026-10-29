using Google.Cloud.Firestore;
using Solvix.Web.Connected;
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
            var codes = new RecordCodes(db, tx);
            var code = await codes.ReserveAsync("", "usr");
            codes.Commit();
            tx.Create(doc,new Dictionary<string,object>{
                {"code",code},
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
            var snapshot = await tx.GetSnapshotAsync(doc);
            if(!snapshot.Exists) throw new ServiceException(404,"User profile not found.");
            if(snapshot.ContainsField("deletionRequestedAt"))
                throw new ServiceException(409,"This account is being deleted. Retry deletion instead of editing or reactivating it.");
            tx.Update(doc,fields);
        });
        return FirestoreMapping.Profile(await doc.GetSnapshotAsync());
    }
    public async Task<bool> PrepareCompanyDeletionAsync(string uid, string companyId)
    {
        var doc = db.Collection("users").Document(ServiceRules.Id(uid));
        var barrier = db.Document("userDeletions/" + uid);
        return await db.RunTransactionAsync(async tx =>
        {
            var profile = await tx.GetSnapshotAsync(doc);
            if (!profile.Exists || FirestoreMapping.Text(profile, "role") != "staff") return false;
            var links = await tx.GetSnapshotAsync(db.Collection("userCompanies/" + uid + "/companies").Limit(2));
            if (links.Documents.Any(d => d.Id != companyId)) return false;
            var existing = await tx.GetSnapshotAsync(barrier);
            if (existing.Exists && (!existing.TryGetValue<string>("json", out var json) || System.Text.Json.JsonSerializer.Deserialize<UserDeletionBarrier>(json)?.CompanyId != companyId)) return false;
            tx.Set(barrier, new Dictionary<string, object> { ["json"] = System.Text.Json.JsonSerializer.Serialize(new UserDeletionBarrier { CompanyId = companyId }) });
            tx.Update(doc, new Dictionary<string, object> { ["status"] = "Inactive", ["deletionRequestedAt"] = FieldValue.ServerTimestamp, ["updatedAt"] = FieldValue.ServerTimestamp });
            return true;
        }, TransactionOptions.ForMaxAttempts(10));
    }
    public async Task UpdateCompanyAsync(string uid, string companyName)
    {
        await UpdateAsync(uid, new Dictionary<string, object> { ["company"] = companyName, ["updatedAt"] = FieldValue.ServerTimestamp });
    }
    public async Task PrepareDeletionAsync(string uid)
    {
        var doc = db.Collection("users").Document(ServiceRules.Id(uid));
        await db.RunTransactionAsync(async tx =>
        {
            if (!(await tx.GetSnapshotAsync(doc)).Exists) return;
            tx.Update(doc, new Dictionary<string, object>
            {
                ["status"] = "Inactive", ["deletionRequestedAt"] = FieldValue.ServerTimestamp,
                ["updatedAt"] = FieldValue.ServerTimestamp
            });
        });
    }
    public async Task DeleteAsync(string uid)
    {
        var doc = db.Collection("users").Document(ServiceRules.Id(uid));
        await db.RunTransactionAsync(async tx =>
        {
            var snapshot = await tx.GetSnapshotAsync(doc);
            if (!snapshot.Exists) return;
            if (!snapshot.ContainsField("deletionRequestedAt"))
                throw new ServiceException(409, "Prepare account deletion before removing its profile.");
            tx.Delete(doc);
        });
    }
}
