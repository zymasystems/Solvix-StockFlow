using System.Globalization;
using System.Text.Json;
using Solvix.Contracts;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.ViewModels.Users;

namespace Solvix.Web.Connected;

// Adapt the original MVC view contracts to the same company ledger used by mobile.
public sealed class CompanyMvcServices(CompanyMvcContext context, TimeProvider clock, IUserRepository accounts) :
    IProductService, ISupplierService, IDashboardService, IReportService, IStockService, ISalesService
{
    private Task<Solvix.Contracts.Workspace> State(bool admin = false) => context.RequireAsync(admin);
    private static PagedResponse<T> Page<T>(IEnumerable<T> source, int size, string? cursor)
    {
        if (size is < 1 or > 100) throw new ServiceException(400, "Page size must be between 1 and 100.");
        int offset = 0;
        if (cursor is not null && (!int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out offset) || offset < 0))
            throw new ServiceException(400, "Invalid page cursor.");
        var rows = source.Skip(offset).Take(size + 1).ToList();
        return new() { Items = rows.Take(size).ToList(), NextCursor = rows.Count > size ? checked(offset + size).ToString(CultureInfo.InvariantCulture) : null };
    }
    private static ProductResponse Product(Solvix.Contracts.Product p, Solvix.Contracts.Workspace s)
    {
        var stock = s.Stock.Where(x => x.ProductId == p.Id).ToList();
        long quantity = stock.Sum(x => x.OnHand), minimum = stock.Sum(x => x.MinStock);
        return new() { Id = p.Id, Code = p.Code, Name = p.Name, Sku = p.Sku, Category = p.Category, Price = p.UnitCost, Stock = quantity, MinStock = minimum, Status = ServiceRules.Status(quantity, minimum) };
    }
    private static T? Display<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return default; }
    }
    private static SupplierResponse Supplier(Solvix.Contracts.Supplier p, Solvix.Contracts.Workspace s)
    {
        var details = Display<SupplierResponse>(p.UiJson);
        return new() { Id = p.Id, Code = p.Code, Name = p.Name, Email = p.Email, Phone = p.Phone, ContactPerson = details?.ContactPerson ?? "", Status = details?.Status ?? "Active", Rating = details?.Rating ?? 0, ProductCount = s.Products.Count(x => x.SupplierId == p.Id) };
    }
    public async Task<ProfileResponse> CurrentProfileAsync()
    {
        var state = await State();
        var account = await accounts.FindAsync(context.Uid)
            ?? throw new ServiceException(403, "Your account profile is unavailable.");
        // Company membership supplies permissions; the registered account supplies personal details.
        // This also fixes existing memberships whose FullName was previously populated with the UID.
        return new ProfileResponse
        {
            Uid = context.Uid, Code = account.Code, DisplayName = AccountDisplayName.From(account),
            FirstName = account.FirstName, LastName = account.LastName,
            Email = account.Email, PhotoUrl = account.PhotoUrl,
            Role = state.Permission.Role, Status = state.Permission.Status, Company = state.Company.Name
        };
    }
    async Task<PagedResponse<ProductResponse>> IProductService.ListAsync(int size, string? cursor)
    { var s = await State(); return Page(s.Products.OrderBy(p => p.Id).Select(p => Product(p, s)), size, cursor); }
    async Task<ProductResponse> IProductService.GetAsync(string id)
    { var s = await State(); return Product(s.Products.FirstOrDefault(p => p.Id == id) ?? throw new ServiceException(404, "Product not found in this company."), s); }
    async Task<ProductResponse> IProductService.CreateAsync(CreateProductRequest r)
    {
        ServiceRules.Validate(r); ServiceRules.Price(r.Price);
        await State(); context.RequireWarehouse();
        var id = context.FormRequestId();
        var created = await context.ExecuteAsync("product.save", new() { RequestId = CompanyMvcContext.Step(id, "product"), Name = r.Name, Sku = r.Sku, Category = r.Category, UnitCost = r.Price });
        await context.ExecuteAsync("stock.configure", new() { RequestId = CompanyMvcContext.Step(id, "settings"), ProductId = created.Id, WarehouseId = context.WarehouseId, MinStock = r.MinStock });
        if (r.OpeningStock > 0)
            await context.ExecuteAsync("receipt.create", new() { RequestId = CompanyMvcContext.Step(id, "opening"), ProductId = created.Id, WarehouseId = context.WarehouseId, Quantity = r.OpeningStock, Reference = "Opening stock", Notes = "Submitted from product intake; arrival, verification and approval required." });
        return new() { Id = created.Id, Code = created.Code, Name = r.Name, Sku = r.Sku, Category = r.Category, Price = r.Price, MinStock = r.MinStock };
    }
    async Task<ProductResponse> IProductService.UpdateAsync(string id, UpdateProductRequest r)
    {
        ServiceRules.Validate(r); ServiceRules.Price(r.Price);
        var s = await State(); var p = s.Products.FirstOrDefault(p => p.Id == id) ?? throw new ServiceException(404, "Product not found.");
        context.RequireWarehouse(); var request = context.FormRequestId();
        await context.ExecuteAsync("product.save", new() { RequestId = CompanyMvcContext.Step(request, "product"), Id = id, Name = r.Name, Sku = r.Sku, Category = r.Category, UnitCost = r.Price, Barcode = p.Barcode, SupplierId = p.SupplierId });
        var stock = s.Stock.FirstOrDefault(x => x.ProductId == id && x.WarehouseId == context.WarehouseId);
        await context.ExecuteAsync("stock.configure", new() { RequestId = CompanyMvcContext.Step(request, "settings"), ProductId = id, WarehouseId = context.WarehouseId, MinStock = r.MinStock, Bin = stock?.Bin ?? "" });
        p.Name = r.Name; p.Sku = r.Sku; p.Category = r.Category; p.UnitCost = r.Price;
        var result = Product(p, s); result.MinStock = r.MinStock; return result;
    }
    async Task IProductService.DeleteAsync(string id) { await State(true); await context.ExecuteAsync("product.archive", new() { Id = id }); }
    async Task<PagedResponse<SupplierResponse>> ISupplierService.ListAsync(int size, string? cursor)
    { var s = await State(); return Page(s.Suppliers.OrderBy(p => p.Id).Select(p => Supplier(p, s)), size, cursor); }
    async Task<SupplierResponse> ISupplierService.GetAsync(string id)
    { var s = await State(); return Supplier(s.Suppliers.FirstOrDefault(p => p.Id == id) ?? throw new ServiceException(404, "Supplier not found in this company."), s); }
    Task<SupplierResponse> ISupplierService.CreateAsync(CreateSupplierRequest r) => SaveSupplier("", r);
    Task<SupplierResponse> ISupplierService.UpdateAsync(string id, UpdateSupplierRequest r) => SaveSupplier(id, r);
    private async Task<SupplierResponse> SaveSupplier(string id, CreateSupplierRequest r)
    {
        ServiceRules.Validate(r); var s = await State(true);
        var previous = s.Suppliers.FirstOrDefault(p => p.Id == id);
        if (id.Length > 0 && previous is null) throw new ServiceException(404, "Supplier not found.");
        var metadata = Display<Dictionary<string, JsonElement>>(previous?.UiJson ?? "{}") ?? new();
        metadata["contactPerson"] = JsonSerializer.SerializeToElement(r.ContactPerson);
        metadata["status"] = JsonSerializer.SerializeToElement(r.Status);
        metadata["rating"] = JsonSerializer.SerializeToElement(r.Rating);
        var saved = await context.ExecuteAsync("supplier.save", new() { Id = id, Name = r.Name, Email = r.Email, Reference = r.Phone, UiJson = JsonSerializer.Serialize(metadata) });
        return new() { Id = saved.Id, Code = saved.Code, Name = r.Name, Email = r.Email, Phone = r.Phone, ContactPerson = r.ContactPerson, Status = r.Status, Rating = r.Rating };
    }
    private static SaleResponse Sale(Movement m, Solvix.Contracts.Workspace s) => new() { Id = m.Id, Code = s.Dispatches.FirstOrDefault(d => d.Id == m.SourceId)?.Code ?? s.Receipts.FirstOrDefault(r => r.Id == m.SourceId)?.Code ?? m.Code, ProductId = m.ProductId, ProductName = s.Products.FirstOrDefault(p => p.Id == m.ProductId)?.Name ?? m.ProductId, Type = m.Kind == "Inbound" ? "Return / receipt" : "Sale / dispatch", Quantity = Math.Abs(m.Delta), PerformedByUserId = m.ActorUid, PerformedByName = s.Members.FirstOrDefault(p => p.Id == m.ActorUid)?.FullName ?? m.ActorUid, OccurredAt = m.At, Notes = m.Reference };
    private static StockAdjustmentResponse Adjustment(Movement m, Solvix.Contracts.Workspace s) => new() { Id = m.Id, Code = m.Code, ProductId = m.ProductId, ProductName = s.Products.FirstOrDefault(p => p.Id == m.ProductId)?.Name ?? m.ProductId, Type = m.Delta >= 0 ? "Add" : "Subtract", Change = m.Delta.ToString("+0;-0;0", CultureInfo.InvariantCulture), PreviousStock = m.Before, NewStock = m.After, Reason = m.Reference, PerformedByUserId = m.ActorUid, PerformedByName = s.Members.FirstOrDefault(p => p.Id == m.ActorUid)?.FullName ?? m.ActorUid, OccurredAt = m.At };
    async Task<PagedResponse<SaleResponse>> ISalesService.ListAsync(int size, string? cursor)
    {
        var s = await State();
        var posted = s.Movements.Where(m => m.Kind is "Inbound" or "Outbound").Select(m => Sale(m, s));
        var pending = s.Dispatches.Where(d => d.Status != "Posted").SelectMany(d => d.Lines.Select(l => new SaleResponse { Id = d.Id, Code = d.Code, ProductName = s.Products.FirstOrDefault(p => p.Id == l.ProductId)?.Name ?? l.ProductId, Type = "Dispatch · " + d.Status, Quantity = l.Quantity, PerformedByName = d.CreatedBy, OccurredAt = d.CreatedAt, Notes = d.Notes }))
            .Concat(s.Receipts.Where(r => r.Status != "Approved").Select(r => new SaleResponse { Id = r.Id, Code = r.Code, ProductName = s.Products.FirstOrDefault(p => p.Id == r.ProductId)?.Name ?? r.ProductId, Type = "Receipt · " + r.Status, Quantity = r.Quantity, PerformedByName = r.CreatedBy, OccurredAt = r.CreatedAt, Notes = r.Notes }));
        return Page(posted.Concat(pending).OrderByDescending(p => p.OccurredAt), size, cursor);
    }
    async Task<SaleResponse> ISalesService.RecordAsync(RecordSaleRequest r)
    {
        ServiceRules.Validate(r); await State(); context.RequireWarehouse();
        if (r.Type == "Transfer" && string.IsNullOrWhiteSpace(r.DestinationWarehouseId)) throw new ServiceException(400, "Choose a destination warehouse for the transfer.");
        var op = new Operation { RequestId = r.RequestId!.Value, WarehouseId = context.WarehouseId, ProductId = r.ProductId, Quantity = r.Quantity, Notes = r.Notes ?? "", Name = "External sale", DestinationWarehouseId = r.Type == "Transfer" ? r.DestinationWarehouseId : null };
        if (r.Type is "Sale" or "Transfer") op.Lines.Add(new() { ProductId = r.ProductId, Quantity = r.Quantity });
        var saved = await context.ExecuteAsync(r.Type == "Return" ? "receipt.create" : "dispatch.create", op);
        return new() { Id = saved.Id, Code = saved.Code, Type = r.Type + " · Pending approval", Quantity = r.Quantity };
    }
    async Task<PagedResponse<StockAdjustmentResponse>> IStockService.ListAsync(int size, string? cursor)
    { var s = await State(); return Page(s.Movements.Where(m => m.Kind is "Adjustment" or "Audit").OrderByDescending(m => m.At).Select(m => Adjustment(m, s)), size, cursor); }
    async Task<StockAdjustmentResponse> IStockService.AdjustAsync(AdjustStockRequest r)
    {
        ServiceRules.Validate(r); var s = await State(true); context.RequireWarehouse();
        var op = new Operation { RequestId = r.RequestId!.Value, ProductId = r.ProductId, WarehouseId = context.WarehouseId, Notes = r.Reason, Reference = r.Reason };
        if (r.Type == "Set")
        {
            var stock = s.Stock.FirstOrDefault(x => x.ProductId == r.ProductId && x.WarehouseId == context.WarehouseId);
            op.Counts.Add(new() { ProductId = r.ProductId, Counted = r.Quantity!.Value, ExpectedVersion = stock?.Version ?? 0 });
            await context.ExecuteAsync("audit.create", op);
        }
        else { op.Quantity = r.Type == "Subtract" ? -r.Quantity!.Value : r.Quantity!.Value; await context.ExecuteAsync("stock.adjust", op); }
        return new();
    }
    async Task<InventoryReportResponse> IReportService.GetInventoryAsync()
    {
        var s = await State(); var products = s.Products.Select(p => Product(p, s)).ToList();
        return new() { GeneratedAt = clock.GetUtcNow(), Products = products, TotalProducts = products.Count, TotalUnits = products.Sum(p => p.Stock), TotalInventoryValue = s.Valuation, LowStockCount = products.Count(p => p.Stock > 0 && p.Stock <= p.MinStock), OutOfStockCount = products.Count(p => p.Stock <= 0) };
    }
    async Task<DashboardResponse> IDashboardService.GetAsync()
    {
        var s = await State(); var report = await ((IReportService)this).GetInventoryAsync();
        return new() { TotalProducts = report.TotalProducts, TotalSuppliers = s.Suppliers.Count, TotalUnits = report.TotalUnits, InventoryValue = report.TotalInventoryValue, LowStockCount = report.LowStockCount, OutOfStockCount = report.OutOfStockCount, LowStockProducts = report.Products.Where(p => p.Stock <= p.MinStock).ToList(), RecentSales = s.Movements.Where(m => m.Kind is "Inbound" or "Outbound").Take(5).Select(m => Sale(m, s)).ToList(), RecentAdjustments = s.Movements.Where(m => m.Kind is "Adjustment" or "Audit").Take(5).Select(m => Adjustment(m, s)).ToList() };
    }
}
