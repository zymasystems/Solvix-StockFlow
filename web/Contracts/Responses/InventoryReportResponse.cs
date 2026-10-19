namespace Solvix.Web.Contracts.Responses;
public class InventoryReportResponse
{
    public DateTimeOffset GeneratedAt { get; set; }
    public int TotalProducts { get; set; }
    public long TotalUnits { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public List<ProductResponse> Products { get; set; } = new();
}
