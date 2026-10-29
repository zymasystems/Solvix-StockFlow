using Google.Api.Gax;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Repositories;
using Solvix.Web.Services;

namespace Solvix.Connected.Tests;

public sealed class CompanyDeletionTests
{
    private sealed class Fixture
    {
        public FirestoreDb Db { get; } = new FirestoreDbBuilder { ProjectId = "demo-delete-company-" + Guid.NewGuid().ToString("N")[..12], EmulatorDetection = EmulatorDetection.EmulatorOnly }.Build();
        public UserRepository Users { get; }
        public FirestoreCompanyStore Store { get; }
        public CompanyService Companies { get; }
        public Directory Identities { get; } = new();
        public CompanyDeletionService Deletion { get; }
        public string CompanyId = "", OtherId = "";
        public Operation Creation = new() { Name = "ePips", RequestId = Guid.NewGuid() };
        public Fixture()
        {
            Users = new(Db); Store = new(Db); Companies = new(Store, TimeProvider.System, Users);
            Deletion = new(Store, Users, Identities, TimeProvider.System);
        }
        public async Task Init()
        {
            foreach (var uid in new[] { "admin", "exclusive", "shared" })
            {
                await Users.CreateAsync(uid, uid + "@example.test", new CreateProfileRequest { FirstName = uid, LastName = "User", Company = "ePips", AcceptTerms = true });
                await Users.UpdateAdminAsync(uid, new() { FirstName = uid, LastName = "User", Company = "ePips", Role = uid == "admin" ? "admin" : "staff", Status = "Active" });
            }
            CompanyId = (await Companies.CreateAsync("admin", Creation)).Id;
            OtherId = (await Companies.CreateAsync("admin", new() { Name = "Pharma", RequestId = Guid.NewGuid() })).Id;
            await Companies.JoinAsync("exclusive", CompanyId, new());
            await Companies.JoinAsync("shared", CompanyId, new());
            await Companies.JoinAsync("shared", OtherId, new());
            await Companies.ExecuteAsync("admin", CompanyId, "product.save", new() { Id = "p", Name = "Motor", Sku = "SKU", RequestId = Guid.NewGuid() });
            await Companies.ExecuteAsync("admin", CompanyId, "stock.adjust", new() { ProductId = "p", WarehouseId = "main", Quantity = 4, Notes = "Verified opening", RequestId = Guid.NewGuid() });
            // Firestore permits missing parent documents with populated descendants.
            await Db.Document($"companies/{CompanyId}/orphanParents/missing/nested/value").SetAsync(new Dictionary<string, object> { ["value"] = "Must also be removed" });
        }
    }

    private sealed class Directory : Solvix.Web.Services.Interfaces.IFirebaseUserDirectory
    {
        public bool Fail;
        public HashSet<string> Deleted = new();
        public Task VerifyExistsAsync(string uid) => Task.CompletedTask;
        public Task DeleteAsync(string uid)
        {
            if (Fail) throw new ServiceException(503, "Retry Firebase deletion.");
            Deleted.Add(uid); return Task.CompletedTask;
        }
    }

    [EmulatorFact]
    public async Task CompanyOnlyDeletionPurgesDescendantsMembershipsAndPreservesAccountsAndOtherTenant()
    {
        var f = new Fixture(); await f.Init();
        var preview = await f.Deletion.PreviewAsync("admin", f.CompanyId);
        Assert.Equal(3, preview.Employees.Count);
        Assert.Contains(preview.Employees, e => e.Uid == "shared" && e.SharedAccount);
        await f.Deletion.DeleteAsync("admin", f.CompanyId, "ePips", false);
        Assert.Null(await f.Store.GetAsync<Company>($"companies/{f.CompanyId}/meta/company"));
        Assert.False((await f.Db.Document($"companies/{f.CompanyId}/warehouses/main/stock/p").GetSnapshotAsync()).Exists);
        Assert.False((await f.Db.Document($"companies/{f.CompanyId}/orphanParents/missing/nested/value").GetSnapshotAsync()).Exists);
        Assert.Empty(await f.Companies.CompaniesAsync("exclusive"));
        Assert.Equal(f.OtherId, Assert.Single(await f.Companies.CompaniesAsync("shared")).Company.Id);
        Assert.NotNull(await f.Users.FindAsync("exclusive"));
        Assert.Equal("", (await f.Users.FindAsync("exclusive"))!.Company);
        Assert.Equal("Pharma", (await f.Users.FindAsync("shared"))!.Company);
        Assert.Empty(f.Identities.Deleted);
        Assert.NotNull(await f.Store.GetAsync<Company>($"companies/{f.OtherId}/meta/company"));
        Assert.Empty(await f.Deletion.PendingAsync("admin"));
        await f.Deletion.DeleteAsync("admin", f.CompanyId, "ePips", false); // Safe retry.
        Assert.Equal(410, (await Assert.ThrowsAsync<ServiceException>(() => f.Companies.CreateAsync("admin", f.Creation))).StatusCode);
        var replacement = await f.Companies.CreateAsync("admin", new() { Name = "ePips", RequestId = Guid.NewGuid() });
        Assert.NotEqual(f.CompanyId, replacement.Id);
        Assert.Equal("cmp-03", replacement.Code);
    }

    [EmulatorFact]
    public async Task ExclusiveAccountDeletionBlocksAccessAndResumesAfterFirebaseFailure()
    {
        var f = new Fixture(); await f.Init(); f.Identities.Fail = true;
        Assert.Equal(503, (await Assert.ThrowsAsync<ServiceException>(() => f.Deletion.DeleteAsync("admin", f.CompanyId, "ePips", true))).StatusCode);
        Assert.Equal("Inactive", (await f.Users.FindAsync("exclusive"))!.Status);
        Assert.Equal(410, (await Assert.ThrowsAsync<ServiceException>(() => f.Companies.LoadAsync("admin", f.CompanyId, ""))).StatusCode);
        Assert.Equal(410, (await Assert.ThrowsAsync<ServiceException>(() => f.Companies.ExecuteAsync("admin", f.CompanyId, "warehouse.save", new() { Name = "Cannot add", RequestId = Guid.NewGuid() }))).StatusCode);
        Assert.Single(await f.Deletion.PendingAsync("admin"));
        Assert.Equal(410, (await Assert.ThrowsAsync<ServiceException>(() => f.Store.RunAsync(tx =>
        {
            tx.Put($"companies/{f.CompanyId}/products/blocked", new Product { Id = "blocked", Name = "Cannot resurrect data" });
            return Task.FromResult(0);
        }))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => f.Deletion.DeleteAsync("admin", f.CompanyId, "ePips", false))).StatusCode);
        f.Identities.Fail = false;
        await f.Deletion.DeleteAsync("admin", f.CompanyId, "ePips", true);
        Assert.Contains("exclusive", f.Identities.Deleted);
        Assert.DoesNotContain("shared", f.Identities.Deleted);
        Assert.DoesNotContain("admin", f.Identities.Deleted);
        Assert.Null(await f.Users.FindAsync("exclusive"));
        Assert.Equal("Active", (await f.Users.FindAsync("shared"))!.Status);
        Assert.Equal("Active", (await f.Users.FindAsync("admin"))!.Status);
        Assert.True((await f.Deletion.PreviewAsync("admin", f.CompanyId)).Completed);
        Assert.Empty(await f.Deletion.PendingAsync("admin"));
    }

    [EmulatorFact]
    public async Task DeletionRequiresSystemAdminAndCorrectConfirmationBeforeAnyWrites()
    {
        var f = new Fixture(); await f.Init();
        await f.Companies.ExecuteAsync("admin", f.CompanyId, "member.review", new() { Id = "exclusive", Role = "admin", Status = "Active", RequestId = Guid.NewGuid() });
        Assert.Equal(403, (await Assert.ThrowsAsync<ServiceException>(() => f.Deletion.DeleteAsync("exclusive", f.CompanyId, "ePips", true))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<ServiceException>(() => f.Deletion.DeleteAsync("admin", f.CompanyId, "Wrong company", true))).StatusCode);
        Assert.Null(await f.Store.GetAsync<CompanyDeletionJob>("companyDeletions/" + f.CompanyId));
        Assert.Equal(4, (await f.Companies.LoadAsync("admin", f.CompanyId, "")).Stock.Single().OnHand);
        Assert.Empty(f.Identities.Deleted);
    }

    [EmulatorFact]
    public async Task AccountPreparationAndConcurrentJoinCannotDeleteASharedAccount()
    {
        var f = new Fixture(); await f.Init();
        async Task<bool> Join()
        {
            try { await f.Companies.JoinAsync("exclusive", f.OtherId, new()); return true; }
            catch (ServiceException e) when (e.StatusCode == 403) { return false; }
        }
        var prepare = f.Users.PrepareCompanyDeletionAsync("exclusive", f.CompanyId);
        var join = Join();
        await Task.WhenAll(prepare, join);
        Assert.NotEqual(prepare.Result, join.Result);
        var links = await f.Store.ListAsync<Entity>("userCompanies/exclusive/companies");
        Assert.Equal(join.Result, links.Any(c => c.Id == f.OtherId));
        Assert.Equal(prepare.Result ? "Inactive" : "Active", (await f.Users.FindAsync("exclusive"))!.Status);
    }
}
