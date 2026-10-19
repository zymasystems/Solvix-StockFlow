using Google.Cloud.Firestore;

namespace Solvix.Web.Models;

[FirestoreData]
public class Sale
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("product")]
    public string ProductName { get; set; } = string.Empty;

    [FirestoreProperty("type")]
    public string Type { get; set; } = string.Empty;

    [FirestoreProperty("qty")]
    public long Quantity { get; set; }

    [FirestoreProperty("by")]
    public string PerformedByName { get; set; } = string.Empty;

    [FirestoreProperty("date")]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty("notes")]
    public string Notes { get; set; } = string.Empty;

    [FirestoreProperty("timestamp")]
    public Timestamp? Timestamp { get; set; }
}