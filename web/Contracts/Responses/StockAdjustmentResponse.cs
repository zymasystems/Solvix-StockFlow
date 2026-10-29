namespace Solvix.Web.Contracts.Responses;

public class StockAdjustmentResponse
{
    public string Code { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;

    public string? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Change { get; set; } = string.Empty;

    public long? PreviousStock { get; set; }

    public long? NewStock { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? PerformedByUserId { get; set; }

    public string PerformedByName { get; set; } = string.Empty;

    public DateTimeOffset? OccurredAt { get; set; }

    public string? LegacyDate { get; set; }

    public Guid? RequestId { get; set; }
}