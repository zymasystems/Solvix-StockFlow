namespace Solvix.Web.Contracts.Responses;

public class SaleResponse
{
    public string Id { get; set; } = string.Empty;

    public string? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public long Quantity { get; set; }

    public string? PerformedByUserId { get; set; }

    public string PerformedByName { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    public string? LegacyDate { get; set; }

    public Guid? RequestId { get; set; }
}