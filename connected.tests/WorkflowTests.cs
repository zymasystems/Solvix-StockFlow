using Google.Api.Gax;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;
using Solvix.Web.Services;

namespace Solvix.Connected.Tests;
public sealed class WorkflowTests
{
    private sealed class Fixture(ICompanyStore store)
    {
        public CompanyService Service { get; } = new(store, TimeProvider.System);

        public string Company = "";
        public async Task Init()
        {
            Company = (await Service.CreateAsync("owner", new() { RequestId = Guid.NewGuid(), Name = "Solvix" })).Id;
            await Do("warehouse.save", new() { Id = "source", Name = "Source" });
            await Do("warehouse.save", new() { Id = "dest", Name = "Destination" });
            await Do("product.save", new() { Id = "p", Sku = "SKU-1", Name = "Motor", Barcode = "123", UnitCost = 2.50m });
        }

        public Task<Result> Do(string command, Operation input, string uid = "owner")
        {
            if (input.RequestId == Guid.Empty)
                input.RequestId = Guid.NewGuid();
            return Service.ExecuteAsync(uid, Company, command, input);
        }

        public Task<Solvix.Contracts.Workspace> Load(string uid = "owner", string warehouse = "") => Service.LoadAsync(uid, Company, warehouse);
        public async Task Staff()
        {
            await Service.JoinAsync("staff", Company, new() { Name = "Staff" });
            await Do("member.review", new() { Id = "staff", Role = "staff", Status = "Active", WarehouseIds = new() { "source" } });
        }

        public async Task<string> Dispatch(long qty, bool transfer = false)
        {
            var id = (await Do("dispatch.create", new() { WarehouseId = "source", DestinationWarehouseId = transfer ? "dest" : null, Lines = new() { new() { ProductId = "p", Quantity = qty } } })).Id;
            await Do("dispatch.pick", new() { Id = id, ProductId = "p", Barcode = "123" });
            await Do("dispatch.stage", new() { Id = id });
            await Do("dispatch.review", new() { Id = id });
            await Do("outbound.verify", new() { Id = id, ProductId = "p", Barcode = "123" });
            return id;
        }
    }

    private static async Task<Fixture> Make(ICompanyStore? store = null)
    {
        var f = new Fixture(store ?? new MemoryStore());
        await f.Init();
        return f;
    }

    private static async Task Denied(Func<Task> action, int status)
    {
        var ex = await Assert.ThrowsAsync<ServiceException>(action);
        Assert.Equal(status, ex.StatusCode);
    }

    [Fact]
    public async Task CompanyAndWarehouseIsolation()
    {
        var f = await Make();
        await f.Staff();
        await Denied(async () => await f.Load("stranger"), 403);
        await Denied(async () => await f.Load("staff", "dest"), 403);
        var own = await f.Load("staff");
        Assert.Single(own.Warehouses);
        await Denied(async () => await f.Do("warehouse.save", new() { Name = "Bad" }, "staff"), 403);
        await f.Do("member.review", new() { Id = "staff", Role = "staff", Status = "Inactive" });
        await Denied(async () => await f.Load("staff"), 403);
    }

    [Fact]
    public async Task ReceivingGateRejectionAndReplay()
    {
        var f = await Make();
        await Receiving(f);
    }

    private static async Task Receiving(Fixture f)
    {
        var id = (await f.Do("receipt.create", new() { WarehouseId = "source", ProductId = "p", Quantity = 5 })).Id;
        Assert.Empty((await f.Load()).Stock);
        await Denied(async () => await f.Do("receipt.approve", new() { Id = id }), 409);
        await f.Do("receipt.arrive", new() { Id = id });
        await f.Do("receipt.verify", new() { Id = id, Barcode = "123" });
        var approval = new Operation
        {
            Id = id,
            RequestId = Guid.NewGuid()
        };
        await f.Do("receipt.approve", approval);
        await f.Do("receipt.approve", approval);
        Assert.Equal(5, (await f.Load()).Stock.Single().OnHand);
        await Denied(async () => await f.Do("receipt.approve", new() { Id = id }), 409);
        var reject = (await f.Do("receipt.create", new() { WarehouseId = "source", ProductId = "p", Quantity = 20 })).Id;
        await f.Do("receipt.reject", new() { Id = reject });
        Assert.Equal(5, (await f.Load()).Stock.Single().OnHand);
        Assert.Single((await f.Load()).Movements);
    }

    [Fact]
    public async Task TransferRequiresDestinationApproval()
    {
        var f = await Make();
        await Transfer(f);
    }

    private static async Task Transfer(Fixture f)
    {
        await f.Do("stock.adjust", new() { WarehouseId = "source", ProductId = "p", Quantity = 10, Notes = "Opening stock" });
        var id = await f.Dispatch(4, true);
        Assert.Equal(10, (await f.Load()).Stock.Single().OnHand);
        await f.Do("outbound.approve", new() { Id = id });
        var state = await f.Load();
        Assert.Equal(6, state.Stock.Single().OnHand);
        Assert.Equal("InTransit", state.Transfers.Single().Status);
        var rid = state.Receipts.Single().Id;
        await f.Do("receipt.arrive", new() { Id = rid });
        await f.Do("receipt.verify", new() { Id = rid, Barcode = "SKU-1" });
        await f.Do("receipt.approve", new() { Id = rid });
        state = await f.Load();
        Assert.Equal(4, state.Stock.Single(s => s.WarehouseId == "dest").OnHand);
        Assert.Equal("Received", state.Transfers.Single().Status);
        Assert.Equal(3, state.Movements.Count);
    }

    [Fact]
    public async Task ConcurrentFinalApprovalsCannotOversell()
    {
        var f = await Make();
        await Concurrent(f);
    }

    private static async Task Concurrent(Fixture f)
    {
        await f.Do("stock.adjust", new() { WarehouseId = "source", ProductId = "p", Quantity = 5, Notes = "Opening" });
        var a = await f.Dispatch(4);
        var b = await f.Dispatch(4);
        async Task<bool> Approve(string id)
        {
            try
            {
                await f.Do("outbound.approve", new() { Id = id });
                return true;
            }
            catch (ServiceException e)when (e.StatusCode == 409)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(Approve(a), Approve(b));
        Assert.Single(results, x => x);
        var state = await f.Load();
        Assert.Equal(1, state.Stock.Single().OnHand);
        Assert.Equal(2, state.Movements.Count);
    }

    [Fact]
    public async Task StaleAuditCannotOverwriteNewStock()
    {
        var f = await Make();
        var aid = (await f.Do("audit.create", new() { WarehouseId = "source", Counts = new() { new() { ProductId = "p", Counted = 3, ExpectedVersion = 0 } } })).Id;
        await f.Do("stock.adjust", new() { WarehouseId = "source", ProductId = "p", Quantity = 8, Notes = "Opening" });
        await Denied(async () => await f.Do("audit.approve", new() { Id = aid }), 409);
        Assert.Equal(8, (await f.Load()).Stock.Single().OnHand);
    }

    [Fact]
    public async Task StaffCannotApproveOrBypassReceivingWithAdjustment()
    {
        var f = await Make();
        await f.Staff();
        var id = (await f.Do("receipt.create", new() { WarehouseId = "source", ProductId = "p", Quantity = 5 }, "staff")).Id;
        await Denied(async () => await f.Do("receipt.approve", new() { Id = id }, "staff"), 403);
        await Denied(async () => await f.Do("stock.adjust", new() { WarehouseId = "source", ProductId = "p", Quantity = 5, Notes = "Bypass" }, "staff"), 403);
        Assert.Empty((await f.Load()).Movements);
    }

    [Fact]
    public async Task ReusedRequestIdWithDifferentPayloadIsRejected()
    {
        var f = await Make();
        var op = new Operation
        {
            RequestId = Guid.NewGuid(),
            WarehouseId = "source",
            ProductId = "p",
            Quantity = 2,
            Notes = "Opening"
        };
        await f.Do("stock.adjust", op);
        op.Quantity = 3;
        await Denied(async () => await f.Do("stock.adjust", op), 409);
        Assert.Equal(2, (await f.Load()).Stock.Single().OnHand);
    }

    [Fact]
    public async Task DuplicateSkuIsCompanyScoped()
    {
        var f = await Make();
        await Denied(async () => await f.Do("product.save", new() { Id = "second", Name = "Duplicate", Sku = "sku-1" }), 409);
        var other = (await f.Service.CreateAsync("owner", new() { RequestId = Guid.NewGuid(), Name = "Other" })).Id;
        await f.Service.ExecuteAsync("owner", other, "product.save", new() { RequestId = Guid.NewGuid(), Name = "Other motor", Sku = "SKU-1" });
        Assert.Single((await f.Load()).Products);
    }

    [Fact]
    public async Task CompletedWorkflowCannotBeOverwrittenByCreate()
    {
        var f = await Make();
        await f.Do("receipt.create", new() { Id = "chosen", WarehouseId = "source", ProductId = "p", Quantity = 2 });
        await f.Do("receipt.reject", new() { Id = "chosen" });
        await Denied(async () => await f.Do("receipt.create", new() { Id = "chosen", WarehouseId = "source", ProductId = "p", Quantity = 100 }), 409);
        Assert.Equal("Rejected", (await f.Load()).Receipts.Single().Status);
    }

    [Fact]
    public async Task OriginalMobileDisplayFieldsPersistWithoutOverridingPermissions()
    {
        var f = await Make();
        await f.Staff();
        await f.Do("company.brand", new() { Name = "Solvix", UiJson = "{\"code\":\"SVX\",\"role\":\"admin\"}", WarehouseId = "dest" });
        await f.Do("warehouse.save", new() { Id = "source", Name = "Source", UiJson = "{\"address\":\"Pretoria\"}" });
        await f.Do("warehouse.save", new() { Id = "dest", Name = "Destination", UiJson = "{\"notes\":\"Private destination notes\"}" });
        await f.Do("product.save", new() { Id = "p", Sku = "SKU-1", Name = "Motor", Barcode = "123", UnitCost = 2.5m, UiJson = "{\"description\":\"Original description\"}" });
        await f.Do("stock.configure", new() { WarehouseId = "source", ProductId = "p", MinStock = 5, UiJson = "{\"binLong\":\"Zone A • Rack 2\"}" });
        var staff = await f.Load("staff");
        Assert.Equal("staff", staff.Permission.Role);
        Assert.Single(staff.Warehouses);
        Assert.Contains("Pretoria", staff.Warehouses.Single().UiJson);
        Assert.DoesNotContain(staff.Warehouses, w => w.UiJson.Contains("Private destination"));
        Assert.Contains("Original description", staff.Products.Single().UiJson);
        Assert.Equal("dest", (await f.Load()).Company.DefaultWarehouseId);
        await Denied(async () => await f.Do("company.brand", new() { Name = "Attack" }, "staff"), 403);
    }

    [Fact]
    public async Task EmployeeMembershipUsesCompanyRolesAndAssignedWarehouse()
    {
        var f = await Make();
        await f.Do("member.add", new() { Id = "new-user", Name = "New employee", Email = "employee@example.test", Role = "admin", WarehouseIds = new() { "source" } });
        var employee = await f.Load("new-user");
        Assert.Equal("staff", employee.Permission.Role);
        Assert.Single(employee.Warehouses);
        await Denied(async () => await f.Load("new-user", "dest"), 403);
        await Denied(async () => await f.Do("member.add", new() { Id = "other", Name = "Other" }, "new-user"), 403);
    }

    [EmulatorFact]
    public async Task MigrationDryRunAndRerunNeverDoublePostOpeningStock()
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = "demo-migrate-" + Guid.NewGuid().ToString("N")[..12],
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
        var store = new FirestoreCompanyStore(db);
        var service = new CompanyService(store, TimeProvider.System);
        var cid = (await service.CreateAsync("owner", new() { RequestId = Guid.NewGuid(), Name = "Legacy target" })).Id;
        await service.ExecuteAsync("owner", cid, "warehouse.save", new() { RequestId = Guid.NewGuid(), Id = "wh", Name = "Main" });
        await db.Document("products/old").SetAsync(new Dictionary<string, object> { { "name", "Motor" }, { "sku", "SKU-OLD" }, { "price", 2.5 }, { "stock", 8L }, { "minStock", 2L }, { "createdAt", 123L } });
        await db.Document("sales/old-sale").SetAsync(new Dictionary<string, object> { { "qty", 2L }, { "timestamp", Timestamp.GetCurrentTimestamp() } });
        await Solvix.Migrate.LegacyMigration.RunAsync(db, cid, "wh", "owner", true, false);
        Assert.Empty((await service.LoadAsync("owner", cid, "wh")).Products);
        await Solvix.Migrate.LegacyMigration.RunAsync(db, cid, "wh", "owner", true, true);
        await Solvix.Migrate.LegacyMigration.RunAsync(db, cid, "wh", "owner", true, true);
        var state = await service.LoadAsync("owner", cid, "wh");
        Assert.Equal(8, state.Stock.Single().OnHand);
        Assert.Equal(20m, state.Valuation);
        Assert.Single(state.Movements);
        Assert.True((await db.Document("products/old").GetSnapshotAsync()).Exists);
        Assert.Equal(2, (await store.ListAsync<Solvix.Migrate.LegacyMigration.Archive>("companies/" + cid + "/legacyArchive")).Count);
    }

    [EmulatorFact]
    public async Task FirestoreRealTransactionsReceivingTransferAndConcurrency()
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = "demo-solvix-" + Guid.NewGuid().ToString("N")[..12],
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
        await Receiving(await Make(new FirestoreCompanyStore(db)));
        await Transfer(await Make(new FirestoreCompanyStore(db)));
        await Concurrent(await Make(new FirestoreCompanyStore(db)));
    }
}

public sealed class EmulatorFactAttribute : FactAttribute
{
    public EmulatorFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")))
            Skip = "Start the Firestore emulator and set FIRESTORE_EMULATOR_HOST.";
    }
}
