using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Solvix.Sims.Models;
using Solvix.Sims.Services.Authentication;
using C = Solvix.Contracts;

namespace Solvix.Sims.Services;
/// <summary>API-backed view data for the supplied mobile screens. Lists are snapshots, never a local stock ledger.</summary>
public sealed class InventoryService : IDisposable
{
    private readonly SessionState session;
    private readonly C.IWorkspaceClient client;
    private readonly IFirebaseAuthService firebase;
    private readonly ApiService api;
    private C.Workspace? snapshot;
    private int generation;
    private readonly Dictionary<string, Guid> pending = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public InventoryService(SessionState session, C.IWorkspaceClient client, IFirebaseAuthService firebase, ApiService api)
    {
        this.session = session;
        this.client = client;
        this.firebase = firebase;
        this.api = api;
        firebase.SignedOut += Clear;
    }

    public event Action? Changed;
    public bool IsLoaded => snapshot is not null;
    public bool IsAdministrator => snapshot?.Permission.Role == "admin";
    public string CurrentCompanyId { get; private set; } = "";
    public string CurrentWarehouse { get; private set; } = "";
    public List<Company> Companies { get; } = new();
    public List<Employee> Employees { get; } = new();
    public List<SupplierContact> SupplierDirectory { get; } = new();
    public List<Product> AllProducts { get; } = new();
    public List<StockMovement> Movements { get; } = new();
    public List<DispatchOrder> DispatchRequests { get; } = new();
    public List<OutboundOrder> OutboundRequests { get; } = new();
    public List<InboundOrder> InboundRequests { get; } = new();
    public Dictionary<string, DispatchOrder> Dispatches { get; } = new();
    public Company CurrentCompany => Companies.FirstOrDefault(c => c.Id == CurrentCompanyId) ?? new Company();
    public IReadOnlyList<Product> Products => AllProducts;
    public IReadOnlyList<Product> WarehouseProducts => AllProducts.Where(p => p.Warehouse == CurrentWarehouse).ToList();
    public int ActiveSkuCount => Products.Select(p => p.ProductId).Distinct().Count();
    public int LowStockCount => Products.Count(p => p.Status == StockStatus.Low);
    public int OutOfStockCount => Products.Count(p => p.Status == StockStatus.OutOfStock);
    public int OverstockedCount => Products.Count(p => p.MinLevel > 0 && p.OnHand >= p.MinLevel * 2L);
    public decimal TotalValuation => Products.Sum(p => p.Valuation);
    public int WarehouseSkuCount => WarehouseProducts.Count;
    public int WarehouseLowStockCount => WarehouseProducts.Count(p => p.Status == StockStatus.Low);
    public int WarehouseOutOfStockCount => WarehouseProducts.Count(p => p.Status == StockStatus.OutOfStock);
    public decimal WarehouseValuation => WarehouseProducts.Sum(p => p.Valuation);
    private IEnumerable<StockMovement> Today => Movements.Where(m => m.At.Date == DateTime.Today);
    public int MovementsToday => Today.Count();
    public int InboundToday => Today.Count(m => m.Kind == MovementKind.Inbound);
    public int OutboundToday => Today.Count(m => m.Kind == MovementKind.Outbound);
    public int WarehouseInboundToday => Today.Count(m => m.Warehouse == CurrentWarehouse && m.Kind == MovementKind.Inbound);
    public int WarehouseOutboundToday => Today.Count(m => m.Warehouse == CurrentWarehouse && m.Kind == MovementKind.Outbound);
    public IEnumerable<StockMovement> Feed => Movements.Where(m => m.Warehouse == CurrentWarehouse).OrderByDescending(m => m.At);

    public IEnumerable<StockMovement> LedgerFor(string sku, string? warehouse = null) => Movements.Where(m => m.Sku == sku && m.Warehouse == (warehouse ?? CurrentWarehouse)).OrderByDescending(m => m.At);
    public IEnumerable<Product> NeedsReorder => WarehouseProducts.Where(p => p.OnHand <= p.MinLevel).OrderBy(p => p.OnHand);

    public Product? Find(string sku, string? warehouse = null) => Products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase) && p.Warehouse == (warehouse ?? CurrentWarehouse)) ?? (warehouse is null ? Products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)) : null);
    public List<Employee> GetEmployeesForCompany() => Employees.ToList();
    public DispatchOrder Dispatch => DispatchRequests.Where(d => d.SourceWarehouse == CurrentWarehouse).OrderBy(d => d.Completed || d.ApprovalStatus == ApprovalStatus.Rejected).ThenByDescending(d => d.CreatedAt).FirstOrDefault() ?? new()
    {
        CompanyId = CurrentCompanyId,
        SourceWarehouse = CurrentWarehouse
    };

    private static T Details<T>(string? json)
        where T : new()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json ?? "{}", Json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    private static string Encode<T>(T value) => JsonSerializer.Serialize(value, Json);
    private static int Units(long value) => checked((int)value);
    private string WarehouseId(string name) => snapshot?.Warehouses.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? throw new ApiException("Choose an assigned warehouse.");
    private string Site(string id) => snapshot?.Warehouses.FirstOrDefault(w => w.Id == id)?.Name ?? id;
    private string ProductId(string sku) => snapshot?.Products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))?.Id ?? throw new ApiException("Product not found. Refresh the catalog.");
    private Guid Request(string key)
    {
        if (!pending.TryGetValue(key, out var id))
            pending[key] = id = Guid.NewGuid();
        return id;
    }

    private async Task<C.Result> Execute(string command, C.Operation input)
    {
        var company = CurrentCompanyId;
        var key = company + ":" + command + ":" + Encode(input);
        input.RequestId = Request(key);
        try
        {
            var result = await client.ExecuteAsync(company, command, input);
            pending.Remove(key);
            return result;
        }
        catch (ApiException ex)when (ex.StatusCode is 401 or 403)
        {
            Clear();
            throw;
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default, string? companyId = null)
    {
        var epoch = ++generation;
        var available = await client.CompaniesAsync(ct);
        var profile = await client.ProfileAsync(ct);
        if (epoch != generation || ct.IsCancellationRequested)
            return;
        var active = available.Where(m => m.Member.Status == "Active").ToList();
        var selected = companyId ?? CurrentCompanyId;
        if (string.IsNullOrEmpty(selected))
            selected = active.FirstOrDefault()?.Company.Id ?? "";
        if (!active.Any(m => m.Company.Id == selected))
        {
            Clear();
            throw new AuthException("MEMBERSHIP_REQUIRED", "Your account is active and you are signed in. Ask a company administrator to activate your company membership and assign your warehouse, then tap Retry.");
        }

        C.Workspace data;
        try
        {
            data = await client.LoadAsync(selected, "", ct: ct);
        }
        catch (ApiException ex)when (ex.StatusCode is 401 or 403)
        {
            Clear();
            throw;
        }

        if (epoch != generation || ct.IsCancellationRequested)
            return;
        snapshot = data;
        CurrentCompanyId = selected;
        if (string.IsNullOrWhiteSpace(profile.FullName))
            profile.FullName = data.Permission.FullName;
        var companies = active.Select(m =>
        {
            var c = Details<Company>(m.Company.UiJson);
            c.Id = m.Company.Id;
            c.Name = m.Company.Name;
            c.LogoUrl = m.Company.LogoUrl;
            c.Warehouses = new();
            c.WarehouseProfiles = new();
            return c;
        }).ToList();
        Companies.Clear();
        Companies.AddRange(companies);
        var company = CurrentCompany;
        company.Warehouses = data.Warehouses.Select(w => w.Name).ToList();
        company.WarehouseProfiles = data.Warehouses.ToDictionary(w => w.Name, w => Details<WarehouseProfile>(w.UiJson), StringComparer.OrdinalIgnoreCase);
        company.DefaultSite = data.Warehouses.FirstOrDefault(w => w.Id == data.Company.DefaultWarehouseId)?.Name ?? company.Warehouses.FirstOrDefault() ?? "";
        if (!company.Warehouses.Contains(CurrentWarehouse))
            CurrentWarehouse = company.DefaultSite;
        AllProducts.Clear();
        foreach (var p in data.Products)
        {
            var stocks = data.Stock.Where(s => s.ProductId == p.Id).ToList();
            if (stocks.Count == 0)
            {
                if (data.Warehouses.FirstOrDefault()is { } w)
                    stocks.Add(new() { ProductId = p.Id, WarehouseId = w.Id });
            }

            foreach (var stock in stocks)
            {
                var item = Details<Product>(p.UiJson);
                item.ProductId = p.Id;
                item.CompanyId = selected;
                item.WarehouseId = stock.WarehouseId;
                item.Version = stock.Version;
                item.Warehouse = Site(stock.WarehouseId);
                item.Name = p.Name;
                item.Sku = p.Sku;
                item.Barcode = p.Barcode;
                item.UnitCost = p.UnitCost;
                item.CategoryLabel = p.Category;
                if (string.IsNullOrWhiteSpace(item.Group))
                    item.Group = p.Category;
                item.OnHand = Units(stock.OnHand);
                item.Reserved = Units(stock.Reserved);
                item.Quarantined = Units(stock.Quarantined);
                item.MinLevel = Units(stock.MinStock);
                item.Bin = stock.Bin;
                var position = Details<Product>(stock.UiJson);
                item.Area = position.Area;
                item.Zone = position.Zone;
                item.BinLong = position.BinLong;
                item.Batch = position.Batch;
                item.MaxCapacity = position.MaxCapacity;
                item.Supplier = data.Suppliers.FirstOrDefault(s => s.Id == p.SupplierId)?.Name ?? item.Supplier;
                AllProducts.Add(item);
            }
        }

        SupplierDirectory.Clear();
        SupplierDirectory.AddRange(data.Suppliers.Select(s =>
        {
            var vendor = Details<SupplierContact>(s.UiJson);
            vendor.Id = s.Id;
            vendor.CompanyId = selected;
            vendor.Name = s.Name;
            vendor.Email = s.Email;
            vendor.Phone = s.Phone;
            return vendor;
        }));
        Employees.Clear();
        Employees.AddRange(data.Members.Select(m => new Employee { Id = m.Id, CompanyId = selected, FullName = m.FullName, Email = m.Email, Role = m.Role, IsActive = m.Status == "Active", AssignedWarehouse = m.Role == "admin" || (data.Warehouses.Count > 0 && data.Warehouses.All(w => m.WarehouseIds.Contains(w.Id))) ? null : m.WarehouseIds.FirstOrDefault()is { } wh ? Site(wh) : null }));
        InboundRequests.Clear();
        InboundRequests.AddRange(data.Receipts.Select(r => new InboundOrder { Id = r.Id, CompanyId = selected, Sku = data.Products.FirstOrDefault(p => p.Id == r.ProductId)?.Sku ?? r.ProductId, ProductName = data.Products.FirstOrDefault(p => p.Id == r.ProductId)?.Name ?? "Archived product", Warehouse = Site(r.WarehouseId), ProductWarehouse = Site(r.WarehouseId), Quantity = Units(r.Quantity), Arrived = r.Arrived, Verified = r.Verified, RequiresArrivalConfirmation = r.TransferId is not null, Reference = r.Reference, Notes = r.Notes, CreatedAt = r.CreatedAt.LocalDateTime, ApprovalStatus = r.Status == "Approved" ? ApprovalStatus.Approved : r.Status == "Rejected" ? ApprovalStatus.Rejected : ApprovalStatus.Pending, ReviewNote = r.ReviewNote }));
        DispatchRequests.Clear();
        OutboundRequests.Clear();
        Dispatches.Clear();
        foreach (var d in data.Dispatches)
        {
            List<DispatchLine> Lines(bool outbound) => d.Lines.Select(l =>
            {
                var p = data.Products.FirstOrDefault(p => p.Id == l.ProductId);
                return new DispatchLine
                {
                    Sku = p?.Sku ?? l.ProductId,
                    Name = p?.Name ?? "Archived product",
                    Qty = Units(l.Quantity),
                    Bin = data.Stock.FirstOrDefault(s => s.ProductId == l.ProductId && s.WarehouseId == d.SourceWarehouseId)?.Bin ?? "",
                    Verified = outbound ? l.VerifiedOutbound : l.Picked
                };
            }).ToList();
            var order = new DispatchOrder
            {
                Id = d.Id,
                CompanyId = selected,
                WorkOrder = d.Reference,
                SourceWarehouse = Site(d.SourceWarehouseId),
                DestinationWarehouse = d.DestinationWarehouseId is null ? "" : Site(d.DestinationWarehouseId),
                Destination = d.Destination,
                Handler = data.Members.FirstOrDefault(m => m.Id == d.HandlerUid)?.FullName ?? d.HandlerUid,
                Note = d.Notes,
                CreatedAt = d.CreatedAt.LocalDateTime,
                Completed = d.Status == "Posted",
                ApprovalStatus = d.Status == "Posted" ? ApprovalStatus.Approved : d.Status == "Rejected" ? ApprovalStatus.Rejected : ApprovalStatus.Pending,
                StagedStatus = d.Status == "Draft" ? StagedDispatchStatus.NotStaged : d.Status == "Staged" ? StagedDispatchStatus.Pending : d.Status == "Rejected" && d.OutboundReviewedBy is null ? StagedDispatchStatus.Rejected : StagedDispatchStatus.Approved,
                Lines = Lines(false)
            };
            DispatchRequests.Add(order);
            if (d.Status is "OutboundPending" or "Posted" || d.OutboundReviewedBy is not null)
                OutboundRequests.Add(new() { Id = d.Id, DispatchId = d.Id, CompanyId = selected, SourceWarehouse = order.SourceWarehouse, DestinationWarehouse = order.DestinationWarehouse, Destination = order.Destination, WorkOrder = d.Reference, Handler = order.Handler, Lines = Lines(true), ApprovalStatus = order.ApprovalStatus, CreatedAt = order.CreatedAt });
        }

        Movements.Clear();
        Movements.AddRange(data.Movements.Select(m => new StockMovement { CompanyId = selected, Warehouse = Site(m.WarehouseId), Sku = data.Products.FirstOrDefault(p => p.Id == m.ProductId)?.Sku ?? m.ProductId, Title = $"{m.Kind}: {data.Products.FirstOrDefault(p => p.Id == m.ProductId)?.Name ?? m.ProductId}", Subtitle = m.Reference, Delta = m.Delta, At = m.At.LocalDateTime, Kind = m.Kind == "Inbound" ? MovementKind.Inbound : m.Kind == "Outbound" ? MovementKind.Outbound : m.Kind == "Audit" ? MovementKind.QualityCheck : MovementKind.Adjustment }));
        if (session.ActiveCompanyId != selected)
            session.SwitchCompany(selected, company.Name, company.Code, CurrentWarehouse, company.Icon);
        session.SignIn(profile.Email, CurrentWarehouse, selected, company.Name, company.Code, company.Icon, profile.FullName, data.Permission.Role);
        session.UpdateProfile(profile.FullName, profile.Email, profile.PhotoUrl ?? "");
        session.SetEmployees(Employees);
        session.SetCurrentEmployee(firebase.UserId ?? "");
        session.SetWarehouse(CurrentWarehouse);
        Changed?.Invoke();
    }

    public bool SelectWarehouse(string warehouse)
    {
        var match = CurrentCompany.Warehouses.FirstOrDefault(w => w.Equals(warehouse, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return false;
        CurrentWarehouse = match;
        session.SetWarehouse(match);
        Changed?.Invoke();
        return true;
    }

    public async Task SelectCompanyAsync(string companyId, SessionState? ignored = null)
    {
        ClearData();
        CurrentCompanyId = companyId;
        await RefreshAsync(companyId: companyId);
    }

    private void ClearData()
    {
        snapshot = null;
        CurrentWarehouse = "";
        AllProducts.Clear();
        Movements.Clear();
        Employees.Clear();
        SupplierDirectory.Clear();
        DispatchRequests.Clear();
        InboundRequests.Clear();
        OutboundRequests.Clear();
        Dispatches.Clear();
        Changed?.Invoke();
    }

    public void Clear()
    {
        generation++;
        ClearData();
        Companies.Clear();
        CurrentCompanyId = "";
        pending.Clear();
    }

    public void Dispose() => firebase.SignedOut -= Clear;
    public async Task AddProductAsync(Product product)
    {
        var id = snapshot?.Products.FirstOrDefault(p => p.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase))?.Id ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CurrentCompanyId + ":" + product.Sku.ToUpperInvariant())))[..32].ToLowerInvariant();
        var supplier = SupplierDirectory.FirstOrDefault(s => s.Name.Equals(product.Supplier, StringComparison.OrdinalIgnoreCase));
        if (product.Supplier.Length > 0 && supplier is null)
        {
            await AddSupplierAsync(new() { Name = product.Supplier, CompanyId = CurrentCompanyId });
            supplier = SupplierDirectory.First(s => s.Name == product.Supplier);
        }

        var result = await Execute("product.save", new() { Id = id, Name = product.Name, Sku = product.Sku, Barcode = product.Barcode, Category = product.CategoryLabel, UnitCost = product.UnitCost, SupplierId = supplier?.Id, UiJson = Encode(new { product.Description, product.Group, product.Icon, product.ImageUrl, product.Supplier, product.SupplierNote, product.LeadTimeDays, product.LeadMode }) });
        product.ProductId = result.Id;
        await Execute("stock.configure", new() { WarehouseId = WarehouseId(product.Warehouse), ProductId = result.Id, MinStock = product.MinLevel, Bin = product.Bin, UiJson = Encode(new { product.Area, product.Zone, product.BinLong, product.Batch, product.MaxCapacity }) });
        await RefreshAsync();
    }

    public async Task CreateInboundRequestAsync(InboundOrder order)
    {
        var form = "receipt-form:" + CurrentCompanyId + ":" + Encode(new { order.Warehouse, order.Sku, order.Quantity, order.Reference, order.Notes });
        var id = Request(form).ToString("N");
        var result = await Execute("receipt.create", new() { Id = id, WarehouseId = WarehouseId(order.Warehouse), ProductId = ProductId(order.Sku), Quantity = order.Quantity, Reference = order.Reference, Notes = order.Notes });
        if (!order.RequiresArrivalConfirmation)
            await Execute("receipt.arrive", new() { Id = result.Id });
        await RefreshAsync();
        pending.Remove(form);
    }

    public async Task<bool> ConfirmInboundArrivalAsync(string id)
    {
        await Execute("receipt.arrive", new() { Id = id });
        await RefreshAsync();
        return true;
    }

    public async Task<bool> VerifyInboundAsync(string id, string code)
    {
        await Execute("receipt.verify", new() { Id = id, Barcode = code });
        await RefreshAsync();
        return true;
    }

    public async Task<bool> ReviewInboundAsync(string id, bool approve, string? note = null)
    {
        await Execute(approve ? "receipt.approve" : "receipt.reject", new() { Id = id, Notes = note ?? "" });
        await RefreshAsync();
        return true;
    }

    public async Task CreateDispatchAsync(DispatchOrder order)
    {
        await Execute("dispatch.create", new() { Id = order.Id, WarehouseId = WarehouseId(order.SourceWarehouse), DestinationWarehouseId = string.IsNullOrWhiteSpace(order.DestinationWarehouse) ? null : WarehouseId(order.DestinationWarehouse), Name = order.Destination, Reference = order.WorkOrder, Notes = order.Note, Lines = order.Lines.Select(l => new C.DispatchLine { ProductId = ProductId(l.Sku), Quantity = l.Qty }).ToList() });
        await RefreshAsync();
    }

    public async Task<bool> VerifyDispatchItemAsync(string code, string sku)
    {
        var id = Dispatch.Id;
        await Execute("dispatch.pick", new() { Id = id, ProductId = ProductId(sku), Barcode = code });
        await RefreshAsync();
        if (Dispatch.Id == id && Dispatch.AllVerified && Dispatch.StagedStatus == StagedDispatchStatus.NotStaged)
        {
            await Execute("dispatch.stage", new() { Id = id });
            await RefreshAsync();
        }

        return true;
    }

    public async Task<bool> ReviewDispatchAsync(string id, bool approve, string? note = null)
    {
        await Execute(approve ? "dispatch.review" : "dispatch.reject", new() { Id = id, Notes = note ?? "" });
        await RefreshAsync();
        return true;
    }

    public async Task<bool> VerifyOutboundItemAsync(string id, string code, string sku)
    {
        await Execute("outbound.verify", new() { Id = id, ProductId = ProductId(sku), Barcode = code });
        await RefreshAsync();
        return true;
    }

    public async Task<bool> ReviewOutboundAsync(string id, bool approve, string? note = null)
    {
        await Execute(approve ? "outbound.approve" : "outbound.reject", new() { Id = id, Notes = note ?? "" });
        await RefreshAsync();
        return true;
    }

    public async Task<int> AdjustAsync(string sku, int delta, string reason, string? reference, string? notes, string? warehouse = null)
    {
        await Execute("stock.adjust", new() { ProductId = ProductId(sku), WarehouseId = WarehouseId(warehouse ?? CurrentWarehouse), Quantity = delta, Reference = reference ?? "", Notes = reason + " " + notes });
        await RefreshAsync();
        return Find(sku, warehouse)?.OnHand ?? 0;
    }

    public async Task<bool> DeleteStockItemAsync(string sku, string warehouse)
    {
        await Execute("stock.delete", new() { ProductId = ProductId(sku), WarehouseId = WarehouseId(warehouse) });
        await RefreshAsync();
        return true;
    }

    public async Task SubmitAuditAsync(string warehouse, Dictionary<string, int> counts)
    {
        await Execute("audit.create", new() { WarehouseId = WarehouseId(warehouse), Counts = Products.Where(p => p.Warehouse == warehouse && counts.ContainsKey(p.Sku)).Select(p => new C.AuditLine { ProductId = p.ProductId, Counted = counts[p.Sku], ExpectedVersion = p.Version }).ToList() });
        await RefreshAsync();
    }

    public async Task AddSupplierAsync(SupplierContact supplier)
    {
        await Execute("supplier.save", new() { Id = supplier.Id, Name = supplier.Name, Email = supplier.Email, Reference = supplier.Phone, UiJson = Encode(supplier) });
        await RefreshAsync();
    }

    public async Task SaveWarehouseAsync(string name, WarehouseProfile profile, string? previousName = null)
    {
        var id = snapshot?.Warehouses.FirstOrDefault(w => w.Name == (previousName ?? name))?.Id ?? "";
        await Execute("warehouse.save", new() { Id = id, Name = name, UiJson = Encode(profile) });
        await RefreshAsync();
    }

    public async Task AddCompanyAsync(Company company, SessionState? ignored = null, Guid? requestId = null)
    {
        var request = requestId ?? Request("new-company:" + company.Name);
        var created = await client.CreateCompanyAsync(new() { Name = company.Name, RequestId = request, Reference = session.FullName });
        var memberships = await client.CompaniesAsync();
        var membership = memberships.FirstOrDefault(m => m.Company.Id == created.Id);
        if (membership?.Member.Status != "Active")
            throw new ApiException("This company already exists. Your membership request is awaiting administrator approval.");
        CurrentCompanyId = created.Id;
        await RefreshAsync(companyId: created.Id);
        // Reusing a company must never overwrite its branding or add the submitted warehouse drafts.
        if (!created.Created || membership.Member.Role != "admin") return;
        var drafts = company.Warehouses.ToList();
        foreach (var site in drafts)
            await SaveWarehouseAsync(site, company.WarehouseProfiles.GetValueOrDefault(site) ?? new());
        company.Id = created.Id;
        await SaveCompanyAsync(company);
        await RefreshAsync();
    }

    public async Task SaveCompanyAsync(Company company)
    {
        var display = new
        {
            company.Code,
            company.Industry,
            company.Description,
            company.Phone,
            company.Address,
            company.Icon,
            company.ThemeColor,
            company.AccentClass,
            company.ContactEmail,
            company.EmailDomain,
            company.CurrencySymbol
        };
        await Execute("company.brand", new() { Name = company.Name, PhotoUrl = company.LogoUrl, Color = snapshot?.Company.Color ?? "#4f46e5", WarehouseId = string.IsNullOrWhiteSpace(company.DefaultSite) ? "" : WarehouseId(company.DefaultSite), UiJson = Encode(display) });
        await RefreshAsync();
    }

    public async Task SaveProfileAsync(string name, string email, string photo)
    {
        await client.SaveProfileAsync(new() { Name = name, PhotoUrl = photo });
        if (!email.Equals(session.Email, StringComparison.OrdinalIgnoreCase))
        {
            await firebase.RequestEmailChangeAsync(email);
            await client.SignOutAsync();
            return;
        }

        await RefreshAsync();
    }

    public async Task AddEmployeeAsync(Employee employee)
    {
        var op = new C.Operation
        {
            RequestId = Request("employee:" + employee.Email),
            Name = employee.FullName,
            Email = employee.Email,
            WarehouseIds = employee.AssignedWarehouse is null ? snapshot!.Warehouses.Select(w => w.Id).ToList() : new()
            {
                WarehouseId(employee.AssignedWarehouse)
            }
        };
        await api.PostAsync<C.Operation, C.Result>($"api/companies/{Uri.EscapeDataString(CurrentCompanyId)}/employees", op);
        await RefreshAsync();
    }

    public async Task AssignEmployeeWarehouseAsync(string id, string? warehouse)
    {
        var member = snapshot?.Members.FirstOrDefault(m => m.Id == id) ?? throw new ApiException("Employee not found.");
        await Execute("member.review", new() { Id = id, Role = member.Role, Status = member.Status, WarehouseIds = string.IsNullOrWhiteSpace(warehouse) ? snapshot!.Warehouses.Select(w => w.Id).ToList() : new() { WarehouseId(warehouse) } });
        await RefreshAsync();
    }
}
