using Google.Api.Gax;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Repositories;

namespace Solvix.Connected.Tests;

public sealed class RecordCodeTests
{
    private static FirestoreDb Database() => new FirestoreDbBuilder
    {
        ProjectId = "demo-codes-" + Guid.NewGuid().ToString("N")[..12],
        EmulatorDetection = EmulatorDetection.EmulatorOnly
    }.Build();

    [EmulatorFact]
    public async Task ConcurrentCreatesPreserveCodesOnReplacementAndNeverReuseDeletedNumbers()
    {
        var db = Database();
        var store = new FirestoreCompanyStore(db);
        await Task.WhenAll(Enumerable.Range(1, 4).Select(i => store.RunAsync(tx =>
        {
            tx.Put("companies/a/suppliers/s" + i, new Supplier { Id = "s" + i, Name = "Supplier " + i, Code = "fake-01" });
            return Task.FromResult(0);
        })));
        var rows = await store.ListAsync<Supplier>("companies/a/suppliers");
        Assert.Equal(new[] { "spr-01", "spr-02", "spr-03", "spr-04" }, rows.Select(s => s.Code).Order().ToArray());
        var first = rows[0];
        await store.RunAsync(tx =>
        {
            tx.Put("companies/a/suppliers/" + first.Id, new Supplier { Id = first.Id, Name = "Edited", Code = "fake-99" });
            return Task.FromResult(0);
        });
        Assert.Equal(first.Code, (await store.GetAsync<Supplier>("companies/a/suppliers/" + first.Id))!.Code);
        await store.RunAsync(tx => { tx.Delete("companies/a/suppliers/" + first.Id); return Task.FromResult(0); });
        await store.RunAsync(tx => { tx.Put("companies/a/suppliers/new", new Supplier { Id = "new" }); return Task.FromResult(0); });
        Assert.Equal("spr-05", (await store.GetAsync<Supplier>("companies/a/suppliers/new"))!.Code);
        Assert.Equal("spr-05", (await db.Document("companies/a/suppliers/new").GetSnapshotAsync()).GetValue<string>("code"));
    }

    [EmulatorFact]
    public async Task CountersAreScopedRollbackIsAtomicAndFormattingContinuesAfter99()
    {
        var db = Database();
        var store = new FirestoreCompanyStore(db);
        await db.Document("companies/a/recordSequences/prd").SetAsync(new Dictionary<string, object> { ["last"] = 99L });
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.RunAsync<int>(tx =>
        {
            tx.Put("companies/a/products/failed", new Product { Id = "failed" });
            throw new InvalidOperationException("abort");
        }));
        await store.RunAsync(tx =>
        {
            tx.Put("companies/a/products/p", new Product { Id = "p" });
            tx.Put("companies/b/products/p", new Product { Id = "p" });
            tx.Put("companies/a/warehouses/w1/stock/p", new Stock { Id = "p", WarehouseId = "w1" });
            tx.Put("companies/a/warehouses/w2/stock/p", new Stock { Id = "p", WarehouseId = "w2" });
            tx.Put("companies/a/meta/company", new Company { Id = "a" });
            tx.Put("companies/b/meta/company", new Company { Id = "b" });
            tx.Put("userCompanies/u/companies/a", new Entity { Id = "a" });
            return Task.FromResult(0);
        });
        Assert.Null(await store.GetAsync<Product>("companies/a/products/failed"));
        Assert.Equal("prd-100", (await store.GetAsync<Product>("companies/a/products/p"))!.Code);
        Assert.Equal("prd-01", (await store.GetAsync<Product>("companies/b/products/p"))!.Code);
        Assert.Equal("stk-01", (await store.GetAsync<Stock>("companies/a/warehouses/w1/stock/p"))!.Code);
        Assert.Equal("stk-02", (await store.GetAsync<Stock>("companies/a/warehouses/w2/stock/p"))!.Code);
        Assert.Equal("cmp-01", (await store.GetAsync<Company>("companies/a/meta/company"))!.Code);
        Assert.Equal("cmp-02", (await store.GetAsync<Company>("companies/b/meta/company"))!.Code);
        Assert.Equal("", (await store.GetAsync<Entity>("userCompanies/u/companies/a"))!.Code);
    }

    [EmulatorFact]
    public async Task RetriedOperationsKeepTheirCodeAndDoNotConsumeAnotherNumber()
    {
        var db = Database();
        var store = new FirestoreCompanyStore(db);
        var service = new CompanyService(store, TimeProvider.System, new MemoryUsersRepository());
        var create = new Operation { RequestId = Guid.NewGuid(), Name = "Company" };
        var cid = (await service.CreateAsync("owner", create)).Id;
        Assert.Equal("cmp-01", (await service.CreateAsync("owner", create)).Code);
        var op = new Operation { RequestId = Guid.NewGuid(), Name = "Supplier" };
        var first = await service.ExecuteAsync("owner", cid, "supplier.save", op);
        Assert.Equal("spr-01", first.Code);
        var replay = await service.ExecuteAsync("owner", cid, "supplier.save", op);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(first.Code, replay.Code);
        Assert.Equal("spr-01", (await service.LoadAsync("owner", cid, "")).Suppliers.Single().Code);
        Assert.Equal(1L, (await db.Document($"companies/{cid}/recordSequences/spr").GetSnapshotAsync()).GetValue<long>("last"));
        Assert.Equal(1L, (await db.Document("recordSequences/cmp").GetSnapshotAsync()).GetValue<long>("last"));
    }

    [EmulatorFact]
    public async Task UserCodesAreGlobalAndIndependentOfFirebaseUid()
    {
        var db = Database();
        var users = new UserRepository(db);
        var request = new CreateProfileRequest { FirstName = "Test", LastName = "User", Company = "A", AcceptTerms = true };
        var first = await users.CreateAsync("firebase-uid-a", "a@example.test", request);
        var second = await users.CreateAsync("firebase-uid-b", "b@example.test", request);
        Assert.Equal("usr-01", first.Code);
        Assert.Equal("usr-02", second.Code);
        Assert.Equal("firebase-uid-a", first.Uid);
        Assert.Equal(first.Code, (await users.UpdateNamesAsync(first.Uid, "New", "Name")).Code);
    }

    [EmulatorFact]
    public async Task BackfillIsDryRunFirstResumableAndPreservesBusinessFields()
    {
        var db = Database();
        var store = new FirestoreCompanyStore(db);
        var service = new CompanyService(store, TimeProvider.System, new MemoryUsersRepository());
        var cid = (await service.CreateAsync("owner", new() { RequestId = Guid.NewGuid(), Name = "Existing company" })).Id;
        var path = $"companies/{cid}/suppliers/original-id";
        await db.Document(path).SetAsync(new Dictionary<string, object>
        {
            ["json"] = System.Text.Json.JsonSerializer.Serialize(new Supplier { Id = "original-id", Name = "Existing supplier", Phone = "0123456789" })
        });
        await db.Document("users/original-uid").SetAsync(new Dictionary<string, object> { ["fullName"] = "Original User", ["status"] = "Active" });
        Assert.Equal(2, await Solvix.Migrate.RecordCodeBackfill.RunAsync(db, cid, true, false));
        Assert.Equal("", (await store.GetAsync<Supplier>(path))!.Code);
        Assert.False((await db.Document("users/original-uid").GetSnapshotAsync()).ContainsField("code"));
        Assert.Equal(2, await Solvix.Migrate.RecordCodeBackfill.RunAsync(db, cid, true, true));
        Assert.Equal(0, await Solvix.Migrate.RecordCodeBackfill.RunAsync(db, cid, true, true));
        var supplier = (await store.GetAsync<Supplier>(path))!;
        Assert.Equal("spr-01", supplier.Code);
        Assert.Equal("original-id", supplier.Id);
        Assert.Equal("Existing supplier", supplier.Name);
        Assert.Equal("0123456789", supplier.Phone);
        var user = await db.Document("users/original-uid").GetSnapshotAsync();
        Assert.Equal("usr-01", user.GetValue<string>("code"));
        Assert.Equal("Active", user.GetValue<string>("status"));
    }
}
