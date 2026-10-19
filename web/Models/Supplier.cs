using Google.Cloud.Firestore;

namespace Solvix.Web.Models;

[FirestoreData]
public class Supplier
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("name")]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty("contact")]
    public string ContactPerson { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty("phone")]
    public string Phone { get; set; } = string.Empty;

    [FirestoreProperty("products")]
    public long ProductCount { get; set; }

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("rating")]
    public double Rating { get; set; }

    [FirestoreProperty("createdAt")]
    public Timestamp? CreatedAt { get; set; }
}