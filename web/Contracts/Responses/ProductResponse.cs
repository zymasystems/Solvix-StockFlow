namespace Solvix.Web.Contracts.Responses;

public class ProductResponse
{
    public string Code { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public long Stock { get; set; }

    public long MinStock { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAt { get; set; }
}