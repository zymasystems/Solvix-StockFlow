namespace Solvix.Web.Contracts.Responses;

public class SupplierResponse
{
    public string? WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public long ProductCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public double Rating { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
}