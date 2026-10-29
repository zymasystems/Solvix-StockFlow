using Google.Cloud.Firestore;

namespace Solvix.Web.Models;

[FirestoreData]
public class UserProfile
{
    [FirestoreProperty("code")]
    public string Code { get; set; } = string.Empty;

    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("uid")]
    public string Uid { get; set; } = string.Empty;

    [FirestoreProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [FirestoreProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [FirestoreProperty("fullName")]
    public string FullName { get; set; } = string.Empty;

    [FirestoreProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty("company")]
    public string Company { get; set; } = string.Empty;

    [FirestoreProperty("role")]
    public string Role { get; set; } = string.Empty;

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("photoURL")]
    public string? PhotoUrl { get; set; }

    [FirestoreProperty("createdAt")]
    public Timestamp? CreatedAt { get; set; }

    [FirestoreProperty("lastLogin")]
    public Timestamp? LastLogin { get; set; }

    // Display helper; not saved to Firestore.
    public string PreferredName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FullName))
                return FullName;

            if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;

            var name = $"{FirstName} {LastName}".Trim();

            return string.IsNullOrWhiteSpace(name) ? Email : name;
        }
    }
}