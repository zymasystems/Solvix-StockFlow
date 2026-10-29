namespace Solvix.Contracts;
public class Entity
{
    public string Id { get; set; } = "";
    // Human-readable reference only. Id remains the stable routing/relationship key.
    public string Code { get; set; } = "";
}

public sealed class Company : Entity
{
    public DateTimeOffset? CreatedAt { get; set; }
    public string DefaultWarehouseId { get; set; } = "";
    public string Name { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string Color { get; set; } = "#4f46e5";
    public string UiJson { get; set; } = "{}";
}

public sealed class Member : Entity
{
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
    public string Role { get; set; } = "staff";
    public string Status { get; set; } = "Pending";
    public List<string> WarehouseIds { get; set; } = new();
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class Membership
{
    public Company Company { get; set; } = new();
    public Member Member { get; set; } = new();
}

public sealed class Warehouse : Entity
{
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public string UiJson { get; set; } = "{}";
}

public sealed class Product : Entity
{
    public string CompanyId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal UnitCost { get; set; }
    public string? SupplierId { get; set; }
    public bool Active { get; set; } = true;
    public string UiJson { get; set; } = "{}";
}

public sealed class Stock : Entity
{
    public string UiJson { get; set; } = "{}";
    public string CompanyId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public long OnHand { get; set; }
    public long Reserved { get; set; }
    public long Quarantined { get; set; }
    public long MinStock { get; set; }
    public string Bin { get; set; } = "";
    public long Version { get; set; }
    public long Available => OnHand - Reserved - Quarantined;
    public bool NeedsReorder => OnHand <= MinStock;
}

public sealed class Supplier : Entity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string UiJson { get; set; } = "{}";
}

public sealed class Receipt : Entity
{
    public string WarehouseId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public long Quantity { get; set; }
    public string Status { get; set; } = "Pending";
    public bool Arrived { get; set; }
    public bool Verified { get; set; }
    public string? TransferId { get; set; }
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public string? VerifiedBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string ReviewNote { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DispatchLine
{
    public string ProductId { get; set; } = "";
    public long Quantity { get; set; }
    public bool Picked { get; set; }
    public bool VerifiedOutbound { get; set; }
}

public sealed class Dispatch : Entity
{
    public string SourceWarehouseId { get; set; } = "";
    public string? DestinationWarehouseId { get; set; }
    public string Destination { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public string HandlerUid { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public string CreatedBy { get; set; } = "";
    public string? StagedReviewedBy { get; set; }
    public string? OutboundReviewedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<DispatchLine> Lines { get; set; } = new();
}

public sealed class Transfer : Entity
{
    public string SourceWarehouseId { get; set; } = "";
    public string DestinationWarehouseId { get; set; } = "";
    public string Status { get; set; } = "InTransit";
    public List<string> ReceiptIds { get; set; } = new();
}

public sealed class Movement : Entity
{
    public string SourceId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Kind { get; set; } = "";
    public long Delta { get; set; }
    public long Before { get; set; }
    public long After { get; set; }
    public string ActorUid { get; set; } = "";
    public string Reference { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

public sealed class Audit : Entity
{
    public string WarehouseId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public List<AuditLine> Lines { get; set; } = new();
}

public sealed class AuditLine
{
    public string ProductId { get; set; } = "";
    public long Counted { get; set; }
    public long ExpectedVersion { get; set; }
}

public sealed class Profile
{
    public string Code { get; set; } = "";
    public string Uid { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhotoUrl { get; set; }
}

public sealed class Operation
{
    public Guid RequestId { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public string? DestinationWarehouseId { get; set; }
    public string ProductId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal UnitCost { get; set; }
    public long Quantity { get; set; }
    public long MinStock { get; set; }
    public long ExpectedVersion { get; set; }
    public string Bin { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Role { get; set; } = "staff";
    public string Status { get; set; } = "Active";
    public string Email { get; set; } = "";
    public string Color { get; set; } = "#4f46e5";
    public string? PhotoUrl { get; set; }
    public string? SupplierId { get; set; }
    public List<string> WarehouseIds { get; set; } = new();
    public List<DispatchLine> Lines { get; set; } = new();
    public List<AuditLine> Counts { get; set; } = new();
    public string? UiJson { get; set; }
}

public sealed class Result
{
    public bool Created { get; set; }
    public string MembershipStatus { get; set; } = "";
    public string Code { get; set; } = "";
    public string Id { get; set; } = "";
    public string Message { get; set; } = "Saved.";
}

public sealed class Workspace
{
    public Member Permission { get; set; } = new();
    public Company Company { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Stock> Stock { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public List<Supplier> Suppliers { get; set; } = new();
    public List<Receipt> Receipts { get; set; } = new();
    public List<Dispatch> Dispatches { get; set; } = new();
    public List<Transfer> Transfers { get; set; } = new();
    public List<Audit> Audits { get; set; } = new();
    public List<Movement> Movements { get; set; } = new();
    public decimal Valuation => Stock.Sum(s => s.OnHand * (Products.FirstOrDefault(p => p.Id == s.ProductId)?.UnitCost ?? 0));
}

public interface IWorkspaceClient
{
    Task<List<Membership>> CompaniesAsync(CancellationToken ct = default);
    Task<Workspace> LoadAsync(string companyId, string warehouseId, string? search = null, CancellationToken ct = default);
    Task<Result> CreateCompanyAsync(Operation input, CancellationToken ct = default);
    Task<Result> JoinAsync(string companyId, Operation input, CancellationToken ct = default);
    Task<Result> ExecuteAsync(string companyId, string command, Operation input, CancellationToken ct = default);
    Task<Profile> ProfileAsync(CancellationToken ct = default);
    Task SaveProfileAsync(Operation input, CancellationToken ct = default);
    Task ChangeEmailAsync(string email, string password, CancellationToken ct = default);
    Task SignOutAsync();
}

public sealed class ResourcePage
{
    public List<System.Text.Json.JsonElement> Items { get; set; } = new();
    public string? NextCursor { get; set; }
}
