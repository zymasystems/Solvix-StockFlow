using Google.Cloud.Firestore;

namespace Solvix.Web.Models;

[FirestoreData]
public class StockAdjustment
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("product")]
    public string ProductName { get; set; } = string.Empty;

    [FirestoreProperty("type")]
    public string Type { get; set; } = string.Empty;

    [FirestoreProperty("change")]
    public string Change { get; set; } = string.Empty;

    [FirestoreProperty("reason")]
    public string Reason { get; set; } = string.Empty;

    [FirestoreProperty("by")]
    public string PerformedByName { get; set; } = string.Empty;

    [FirestoreProperty("date")]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty("timestamp")]
    public Timestamp? Timestamp { get; set; }
}