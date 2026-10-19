using Google.Cloud.Firestore;

namespace Solvix.Web.Models;

[FirestoreData]
public class Product
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("name")]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty("sku")]
    public string Sku { get; set; } = string.Empty;

    [FirestoreProperty("category")]
    public string Category { get; set; } = string.Empty;

    [FirestoreProperty("price")]
    public double Price { get; set; }

    [FirestoreProperty("stock")]
    public long Stock { get; set; }

    [FirestoreProperty("minStock")]
    public long MinStock { get; set; }

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public Timestamp? CreatedAt { get; set; }

    // Calculated in C#; not stored in Firestore.
    public string CalculatedStatus =>
        Stock <= 0 ? "Out of Stock" :
        Stock <= MinStock ? "Low Stock" :
        "In Stock";
}