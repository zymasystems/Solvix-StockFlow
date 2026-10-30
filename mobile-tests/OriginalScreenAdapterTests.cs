using System.Net;
using System.Text.Json;
using Solvix.Contracts;
using Solvix.Sims.Services;
using Solvix.Sims.Services.Authentication;

namespace Solvix.Mobile.Auth.Tests;
public sealed class OriginalScreenAdapterTests
{
    private sealed class Auth : IFirebaseAuthService
    {
        public string? UserId => "owner";

        public event Action? SignedOut;
        public Task<string> GetIdTokenAsync(CancellationToken ct = default) => Task.FromResult("token");
        public Task SignInAsync(string e, string p, bool remember, CancellationToken ct = default) => Task.CompletedTask;
        public Task RegisterAsync(string e, string p, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> RestoreAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task SendPasswordResetAsync(string e, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequestEmailChangeAsync(string e, CancellationToken ct = default) => Task.CompletedTask;
        public Task SignOutAsync()
        {
            SignedOut?.Invoke();
            return Task.CompletedTask;
        }
    }

    private sealed class Online : IConnectionStatus
    {
        public bool IsConnected => true;
    }

    private sealed class NoHttp : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => throw new InvalidOperationException("Unexpected direct HTTP");
    }

    private sealed class Client : IWorkspaceClient
    {
        public List<(string Command, Operation Input)> Calls = new();
        public bool Fail;
        public int? FailStatus;
        public List<Membership> Memberships = new()
        {
            new()
            {
                Company = new()
                {
                    Id = "a",
                    Name = "Company A",
                    UiJson = "{\"code\":\"A\",\"icon\":\"factory\"}"
                },
                Member = new()
                {
                    Id = "owner",
                    Role = "admin",
                    Status = "Active"
                }
            },
            new()
            {
                Company = new()
                {
                    Id = "b",
                    Name = "Company B"
                },
                Member = new()
                {
                    Id = "owner",
                    Role = "staff",
                    Status = "Active"
                }
            }
        };
        public Func<string, Task<Solvix.Contracts.Workspace>>? Loader;
        public Task<List<Membership>> CompaniesAsync(CancellationToken ct = default) => Task.FromResult(Memberships);
        public Task<Profile> ProfileAsync(CancellationToken ct = default) => Task.FromResult(new Profile { Uid = "owner", FullName = "Mash", Email = "mash@example.test" });
        public Task<Solvix.Contracts.Workspace> LoadAsync(string c, string w, string? search = null, CancellationToken ct = default) => Loader?.Invoke(c) ?? Task.FromResult(Data(c));
        public Solvix.Contracts.Workspace Data(string c) => new()
        {
            Company = Memberships.First(m => m.Company.Id == c).Company,
            Permission = Memberships.First(m => m.Company.Id == c).Member,
            Warehouses = new()
            {
                new()
                {
                    Id = "wh-" + c,
                    Name = "Warehouse " + c,
                    UiJson = "{\"address\":\"Pretoria\"}"
                }
            },
            Products = new()
            {
                new()
                {
                    Id = "p-" + c,
                    CompanyId = c,
                    Name = "Motor " + c,
                    Sku = "SKU-" + c,
                    Barcode = "123",
                    UnitCost = 2m,
                    UiJson = "{\"description\":\"Original product description\",\"onHand\":9999}"
                }
            },
            Stock = new()
            {
                new()
                {
                    ProductId = "p-" + c,
                    WarehouseId = "wh-" + c,
                    OnHand = 8,
                    Reserved = 1,
                    MinStock = 10,
                    Version = 3
                }
            }
        };
        public Task<Result> ExecuteAsync(string c, string command, Operation input, CancellationToken ct = default)
        {
            Calls.Add((command, input));
            if (Fail)
                throw new ApiException("offline", FailStatus);
            return Task.FromResult(new Result { Id = input.Id.Length == 0 ? "created" : input.Id });
        }

        public Result CreationResult = new() { Id = "a", Created = true, MembershipStatus = "Active" };
        public Task<Result> CreateCompanyAsync(Operation i, CancellationToken ct = default) => Task.FromResult(CreationResult);
        public Task<Result> JoinAsync(string c, Operation i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveProfileAsync(Operation i, CancellationToken ct = default) => Task.CompletedTask;
        public Task ChangeEmailAsync(string e, string p, CancellationToken ct = default) => Task.CompletedTask;
        public Task SignOutAsync() => Task.CompletedTask;
    }

    private static (InventoryService Inventory, Client Client, Auth Auth, SessionState Session) Setup()
    {
        var client = new Client();
        var auth = new Auth();
        var session = new SessionState();
        var api = new ApiService(new HttpClient(new NoHttp()) { BaseAddress = new Uri("https://example.test/") }, auth, new Online());
        return (new(session, client, auth, api), client, auth, session);
    }

    private sealed class ProfileHandler(string role = "staff", string status = "Active", int code = 200) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal("/api/profile", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)code)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    uid = "owner", email = "mash@example.test", displayName = "Mash", role, status
                }), System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task ApprovedAccountWithoutMembershipStaysSignedInAndRetryLoadsAssignedWorkspace(string role)
    {
        var f = Setup();
        var membership = f.Client.Memberships[0];
        f.Client.Memberships.Clear();
        var api = new ApiService(new HttpClient(new ProfileHandler(role)) { BaseAddress = new("https://example.test/") }, f.Auth, new Online());
        var coordinator = new MobileAuthCoordinator(f.Auth, api, f.Session);

        await coordinator.SignInAsync("mash@example.test", "password", false);
        Assert.True(coordinator.IsApproved);
        Assert.True(f.Session.IsSignedIn);
        var error = await Assert.ThrowsAsync<AuthException>(() => f.Inventory.RefreshAsync());
        Assert.Equal("MEMBERSHIP_REQUIRED", error.Code);
        Assert.True(coordinator.IsApproved);
        Assert.True(f.Session.IsSignedIn);
        Assert.False(f.Inventory.IsLoaded);
        Assert.Empty(f.Inventory.Products);

        // Administrator grants company access; Retry reloads permissions without another login.
        f.Client.Memberships.Add(membership);
        await coordinator.InitializeAsync();
        await f.Inventory.RefreshAsync();
        Assert.True(f.Inventory.IsLoaded);
        Assert.Equal("a", f.Inventory.CurrentCompanyId);
        Assert.NotEmpty(f.Inventory.Products);

        await coordinator.SignOutAsync();
        Assert.False(coordinator.IsApproved);
        Assert.False(f.Session.IsSignedIn);
        Assert.False(f.Inventory.IsLoaded);
        Assert.Empty(f.Inventory.Products);
    }

    [Theory]
    [InlineData("staff", "Inactive", 200)]
    [InlineData("staff", "Pending", 200)]
    [InlineData("owner", "Active", 200)]
    [InlineData("staff", "Inactive", 403)]
    public async Task UnapprovedAccountCannotOpenMobileSession(string role, string status, int responseCode)
    {
        var f = Setup();
        var api = new ApiService(new HttpClient(new ProfileHandler(role, status, responseCode)) { BaseAddress = new("https://example.test/") }, f.Auth, new Online());
        var coordinator = new MobileAuthCoordinator(f.Auth, api, f.Session);
        var error = await Assert.ThrowsAsync<AuthException>(() => coordinator.SignInAsync("mash@example.test", "password", false));
        Assert.Equal("APPROVAL_REQUIRED", error.Code);
        Assert.False(coordinator.IsApproved);
        Assert.False(f.Session.IsSignedIn);
        Assert.Null(coordinator.Profile);
        Assert.False(f.Inventory.IsLoaded);
    }

    [Fact]
    public async Task OriginalModelsUseAuthoritativeStockAndKeepPresentationFields()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        var p = Assert.Single(f.Inventory.Products);
        Assert.Equal("Original product description", p.Description);
        Assert.Equal(8, p.OnHand);
        Assert.Equal("p-a", p.ProductId);
        Assert.Equal("wh-a", p.WarehouseId);
        Assert.Equal(3, p.Version);
        Assert.Equal("factory", f.Inventory.CurrentCompany.Icon);
        Assert.Equal("Pretoria", f.Inventory.CurrentCompany.WarehouseProfiles["Warehouse a"].Address);
    }

    [Fact]
    public async Task CompanySwitchClearsPreviousStockBeforeRequestCompletes()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        var gate = new TaskCompletionSource<Solvix.Contracts.Workspace>();
        f.Client.Loader = c => gate.Task;
        var loading = f.Inventory.SelectCompanyAsync("b");
        Assert.Empty(f.Inventory.AllProducts);
        Assert.False(f.Inventory.IsLoaded);
        gate.SetResult(f.Client.Data("b"));
        await loading;
        Assert.All(f.Inventory.Products, p => Assert.Equal("b", p.CompanyId));
        Assert.Equal("staff", f.Session.Role);
    }

    [Fact]
    public async Task SignOutDuringRefreshCannotRepopulatePrivateData()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        var gate = new TaskCompletionSource<Solvix.Contracts.Workspace>();
        f.Client.Loader = c => gate.Task;
        var loading = f.Inventory.RefreshAsync();
        await f.Auth.SignOutAsync();
        gate.SetResult(f.Client.Data("a"));
        await loading;
        Assert.Empty(f.Inventory.AllProducts);
        Assert.Empty(f.Inventory.Companies);
        Assert.False(f.Inventory.IsLoaded);
    }

    [Fact]
    public async Task SubmissionMapsOriginalSkuAndWarehouseToBackendIdsWithoutPostingStock()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        await f.Inventory.CreateInboundRequestAsync(new() { Sku = "SKU-a", Warehouse = "Warehouse a", Quantity = 5, Reference = "PO-1" });
        Assert.Equal(new[] { "receipt.create", "receipt.arrive" }, f.Client.Calls.Select(c => c.Command));
        Assert.Equal("p-a", f.Client.Calls[0].Input.ProductId);
        Assert.Equal("wh-a", f.Client.Calls[0].Input.WarehouseId);
        Assert.Equal(8, f.Inventory.Products.Single().OnHand);
    }

    [Fact]
    public async Task FailedAdjustmentDoesNotMutateTheDisplayedLedger()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        f.Client.Fail = true;
        await Assert.ThrowsAsync<ApiException>(() => f.Inventory.AdjustAsync("SKU-a", -3, "Damaged", null, null));
        Assert.Equal(8, f.Inventory.Products.Single().OnHand);
        Assert.Empty(f.Inventory.Movements);
        var first = f.Client.Calls.Single().Input.RequestId;
        await Assert.ThrowsAsync<ApiException>(() => f.Inventory.AdjustAsync("SKU-a", -3, "Damaged", null, null));
        Assert.Equal(first, f.Client.Calls.Last().Input.RequestId);
    }

    [Fact]
    public async Task AuditPostsVersionedCountsInsteadOfLocalAdjustments()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        await f.Inventory.SubmitAuditAsync("Warehouse a", new() { { "SKU-a", 4 } });
        var call = Assert.Single(f.Client.Calls);
        Assert.Equal("audit.create", call.Command);
        var count = Assert.Single(call.Input.Counts);
        Assert.Equal(3, count.ExpectedVersion);
        Assert.Equal(4, count.Counted);
        Assert.Equal(8, f.Inventory.Products.Single().OnHand);
    }

    [Fact]
    public async Task PendingMembershipDoesNotCreateOrSelectDemoCompany()
    {
        var f = Setup();
        f.Client.Memberships.ForEach(m => m.Member.Status = "Pending");
        await Assert.ThrowsAsync<AuthException>(() => f.Inventory.RefreshAsync());
        Assert.Empty(f.Inventory.AllProducts);
        Assert.Empty(f.Inventory.Companies);
        Assert.Equal("", f.Inventory.CurrentCompanyId);
    }

    [Fact]
    public async Task RevokedMembershipClearsPreviouslyLoadedData()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        f.Client.Memberships[0].Member.Status = "Inactive";
        await Assert.ThrowsAsync<AuthException>(() => f.Inventory.RefreshAsync());
        Assert.Empty(f.Inventory.AllProducts);
        Assert.False(f.Inventory.IsLoaded);
    }

    [Fact]
    public async Task PermissionFailureClearsCachedWorkspaceWithoutChangingAuthentication()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        f.Client.Fail = true;
        f.Client.FailStatus = 403;
        await Assert.ThrowsAsync<ApiException>(() => f.Inventory.AdjustAsync("SKU-a", -1, "Count", null, null));
        Assert.False(f.Inventory.IsLoaded);
        Assert.Empty(f.Inventory.AllProducts);
        Assert.Equal("owner", f.Auth.UserId);
    }

    [Fact]
    public async Task RetryingOriginalReceiptFormUsesStableBackendIdentifier()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync();
        f.Client.Fail = true;
        async Task Submit() => await f.Inventory.CreateInboundRequestAsync(new() { Sku = "SKU-a", Warehouse = "Warehouse a", Quantity = 2, Reference = "PO" });
        await Assert.ThrowsAsync<ApiException>(Submit);
        await Assert.ThrowsAsync<ApiException>(Submit);
        Assert.Equal(f.Client.Calls[0].Input.Id, f.Client.Calls[1].Input.Id);
        Assert.Equal(f.Client.Calls[0].Input.RequestId, f.Client.Calls[1].Input.RequestId);
    }
    [Fact]
    public async Task ExistingCompanyNameDoesNotOverwriteBrandingOrCreateSubmittedWarehouses()
    {
        var f = Setup();
        f.Client.CreationResult = new() { Id = "a", Created = false, MembershipStatus = "Active" };
        await f.Inventory.AddCompanyAsync(new Solvix.Sims.Models.Company { Name = "Company A", Warehouses = new() { "Unwanted warehouse" } });
        Assert.Equal("a", f.Inventory.CurrentCompanyId);
        Assert.Empty(f.Client.Calls);
    }

    [Fact]
    public async Task PendingCompanyRequestKeepsCurrentCompanyAndDoesNotWriteToTheRequestedCompany()
    {
        var f = Setup();
        await f.Inventory.RefreshAsync(companyId: "b");
        f.Client.CreationResult = new() { Id = "a", Created = false, MembershipStatus = "Pending" };
        f.Client.Memberships[0].Member.Status = "Pending";
        var error = await Assert.ThrowsAsync<ApiException>(() => f.Inventory.AddCompanyAsync(new Solvix.Sims.Models.Company { Name = "Company A" }));
        Assert.Contains("awaiting administrator approval", error.Message);
        Assert.Equal("b", f.Inventory.CurrentCompanyId);
        Assert.Empty(f.Client.Calls);
    }

    [Fact]
    public async Task DeletedCompanyResponseClearsCachedInventoryWithoutCrashingOrSigningOutSharedUser()
    {
        var f = Setup(); await f.Inventory.RefreshAsync();
        f.Client.Fail = true; f.Client.FailStatus = 410;
        await Assert.ThrowsAsync<ApiException>(() => f.Inventory.AdjustAsync("SKU-a", -1, "Count", null, null));
        Assert.False(f.Inventory.IsLoaded);
        Assert.Empty(f.Inventory.AllProducts);
        Assert.Empty(f.Inventory.Employees);
        Assert.Equal("owner", f.Auth.UserId);
    }

    [Fact]
    public async Task NavigationReusesLoadedWorkspaceButExplicitRefreshStillFetches()
    {
        var f = Setup(); int loads = 0;
        f.Client.Loader = c => { loads++; return Task.FromResult(f.Client.Data(c)); };
        await f.Inventory.EnsureLoadedAsync();
        for (var route=0; route<10; route++) await f.Inventory.EnsureLoadedAsync();
        Assert.Equal(1, loads); Assert.True(f.Inventory.IsLoaded);
        await f.Inventory.RefreshAsync(); Assert.Equal(2, loads);
        await f.Auth.SignOutAsync(); Assert.False(f.Inventory.IsLoaded);
        await f.Inventory.EnsureLoadedAsync(); Assert.Equal(3, loads);
    }

    [Fact]
    public async Task ConcurrentInitialNavigationLoadsOnceAndFailureCanRetry()
    {
        var f = Setup(); int loads = 0;
        var started = new TaskCompletionSource(); var finish = new TaskCompletionSource();
        f.Client.Loader = async c => { loads++; started.TrySetResult(); await finish.Task; return f.Client.Data(c); };
        var first = f.Inventory.EnsureLoadedAsync(); await started.Task;
        var second = f.Inventory.EnsureLoadedAsync(); finish.SetResult();
        await Task.WhenAll(first, second); Assert.Equal(1, loads);
        f.Inventory.Clear();
        f.Client.Loader = c => throw new ApiException("Connection failed");
        await Assert.ThrowsAsync<ApiException>(() => f.Inventory.EnsureLoadedAsync()); Assert.False(f.Inventory.IsLoaded);
        f.Client.Loader = c => Task.FromResult(f.Client.Data(c));
        await f.Inventory.EnsureLoadedAsync(); Assert.True(f.Inventory.IsLoaded);
    }

}
