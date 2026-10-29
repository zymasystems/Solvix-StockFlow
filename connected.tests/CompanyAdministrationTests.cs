using Google.Api.Gax;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Users;

namespace Solvix.Connected.Tests;

public sealed class CompanyAdministrationTests
{
    [Fact]
    public async Task ApprovalReusesCompanyAssignsDefaultWarehouseAndKeepsOtherCompanyIsolated()
    {
        var users = new MemoryUsersRepository();
        var store = new MemoryStore();
        var service = new CompanyService(store, TimeProvider.System, users);
        await users.UpdateAdminAsync("new-user", new() { FirstName = "First", LastName = "Employee", Company = "ePips", Status = "Active", Role = "staff" });
        await users.UpdateAdminAsync("staff", new() { FirstName = "Second", LastName = "Employee", Company = "  EPIPS  ", Status = "Active", Role = "staff" });
        await users.UpdateAdminAsync("other", new() { FirstName = "Other", LastName = "Employee", Company = "Pharma Systems", Status = "Active", Role = "staff" });
        await service.ApproveAccountCompanyAsync("platform-admin", "new-user", "staff");
        await service.ApproveAccountCompanyAsync("platform-admin", "staff", "admin");
        await service.ApproveAccountCompanyAsync("platform-admin", "other", "staff");
        var first = Assert.Single(await service.CompaniesAsync("new-user"));
        var second = Assert.Single(await service.CompaniesAsync("staff"));
        var pharma = Assert.Single(await service.CompaniesAsync("other"));
        Assert.Equal(first.Company.Id, second.Company.Id);
        Assert.NotEqual(first.Company.Id, pharma.Company.Id);
        Assert.Equal("admin", second.Member.Role);
        Assert.Equal("staff", (await users.FindAsync("staff"))!.Role); // Not a system administrator.
        var warehouse = Assert.Single((await service.LoadAsync("new-user", first.Company.Id, "")).Warehouses);
        Assert.Equal(first.Company.DefaultWarehouseId, warehouse.Id);
        await service.ExecuteAsync("platform-admin", first.Company.Id, "product.save", new() { Id = "motor", Name = "Motor", Sku = "EP-1", RequestId = Guid.NewGuid() });
        await service.ExecuteAsync("platform-admin", first.Company.Id, "stock.adjust", new() { ProductId = "motor", WarehouseId = warehouse.Id, Quantity = 5, Notes = "Verified stock", RequestId = Guid.NewGuid() });
        Assert.Equal(5, (await service.LoadAsync("new-user", first.Company.Id, warehouse.Id)).Stock.Single().OnHand);
        Assert.Empty((await service.LoadAsync("other", pharma.Company.Id, "")).Products);
        Assert.Equal(403, (await Assert.ThrowsAsync<ServiceException>(() => service.LoadAsync("other", first.Company.Id, warehouse.Id))).StatusCode);
        var panels = await service.ManagementPanelsAsync("platform-admin");
        Assert.Equal(2, panels.Count);
        Assert.Contains(panels, p => p.Company.Name == "ePips" && p.Members.Any(m => m.FullName == "First Employee"));
        Assert.Single(await service.ManagementPanelsAsync("new-user"));
    }

    [Fact]
    public async Task NameEntryRequestsMembershipWithoutPromotingAnExistingCompanyMember()
    {
        var service = new CompanyService(new MemoryStore(), TimeProvider.System, new MemoryUsersRepository());
        var original = await service.CreateAsync("owner", new() { Name = "Zara   Systems", RequestId = Guid.NewGuid() });
        var join = await service.CreateAsync("new-user", new() { Name = " zara systems ", RequestId = Guid.NewGuid() });
        Assert.Equal(original.Id, join.Id);
        Assert.False(join.Created);
        Assert.Equal("Pending", join.MembershipStatus);
        var member = Assert.Single(await service.CompaniesAsync("new-user"));
        Assert.Equal("staff", member.Member.Role);
        Assert.Equal(403, (await Assert.ThrowsAsync<ServiceException>(() => service.LoadAsync("new-user", original.Id, ""))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<ServiceException>(() => service.ApproveAccountCompanyAsync("new-user", "staff", "admin"))).StatusCode);
        var owner = await service.LoadAsync("owner", original.Id, "");
        Assert.Single(owner.Warehouses);
        Assert.Equal(original.Id, owner.Company.Id);
    }

    [Fact]
    public async Task RenameCannotTakeAnExistingNameAndLegacyCompaniesAreReused()
    {
        var store = new MemoryStore();
        var service = new CompanyService(store, TimeProvider.System, new MemoryUsersRepository());
        await store.RunAsync(tx =>
        {
            tx.Put("companies/legacy/meta/company", new Company { Id = "legacy", Name = "Legacy Business" });
            return Task.FromResult(0);
        });
        var legacy = await service.CreateAsync("owner", new() { Name = " LEGACY business ", RequestId = Guid.NewGuid() });
        Assert.Equal("legacy", legacy.Id);
        Assert.Equal("Pending", legacy.MembershipStatus);
        var other = await service.CreateAsync("owner", new() { Name = "Other", RequestId = Guid.NewGuid() });
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => service.ExecuteAsync("owner", other.Id, "company.brand", new() { Name = "legacy business", RequestId = Guid.NewGuid() }))).StatusCode);
        await service.ExecuteAsync("owner", other.Id, "company.brand", new() { Name = "Renamed", RequestId = Guid.NewGuid() });
        Assert.Equal(other.Id, (await service.CreateAsync("staff", new() { Name = "renamed", RequestId = Guid.NewGuid() })).Id);
    }

    [EmulatorFact]
    public async Task ConcurrentNameRegistrationCreatesOneCompanyAndOneDefaultWarehouse()
    {
        var db = new FirestoreDbBuilder { ProjectId = "demo-company-" + Guid.NewGuid().ToString("N")[..12], EmulatorDetection = EmulatorDetection.EmulatorOnly }.Build();
        var service = new CompanyService(new FirestoreCompanyStore(db), TimeProvider.System, new MemoryUsersRepository());
        var tasks = new[] { "owner", "new-user", "staff" }.Select(uid => service.CreateAsync(uid, new() { Name = "  ePips ", RequestId = Guid.NewGuid() })).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Single(results.Select(r => r.Id).Distinct());
        Assert.Single(results, r => r.Created);
        var creator = new[] { "owner", "new-user", "staff" }[Array.FindIndex(results, r => r.Created)];
        var state = await service.LoadAsync(creator, results[0].Id, "");
        Assert.Single(state.Warehouses);
        Assert.Equal(state.Warehouses.Single().Id, state.Company.DefaultWarehouseId);
        Assert.Equal("cmp-01", state.Company.Code);
        Assert.Equal(3, state.Members.Count);
    }
}
