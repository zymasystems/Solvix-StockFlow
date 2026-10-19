using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;
public interface IConnectedProfiles
{
    Task<Profile> GetAsync(string uid);
    Task SaveAsync(string uid, Operation input);
}

public sealed class ConnectedProfiles(FirestoreDb db, FirebaseAuth auth) : IConnectedProfiles
{
    public async Task<Profile> GetAsync(string uid)
    {
        var identity = await auth.GetUserAsync(uid);
        var doc = await db.Collection("users").Document(CompanyService.Id(uid)).GetSnapshotAsync();
        string Read(string field) => doc.Exists && doc.TryGetValue<string>(field, out var value) ? value : "";
        return new Profile
        {
            Uid = uid,
            Email = identity.Email ?? "",
            FullName = Read("fullName"),
            PhotoUrl = Read("photoURL")
        };
    }

    public async Task SaveAsync(string uid, Operation input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 200)
            throw new ServiceException(400, "Enter a name of up to 200 characters.");
        var photo = input.PhotoUrl ?? "";
        if (photo.StartsWith("data:", StringComparison.Ordinal))
        {
            var prefix = photo.StartsWith("data:image/png;base64,", StringComparison.Ordinal) ? "data:image/png;base64," : photo.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal) ? "data:image/jpeg;base64," : "";
            if (prefix.Length == 0 || photo.Length > 280000)
                throw new ServiceException(400, "Use a PNG/JPEG image of at most 200 KB.");
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(photo[prefix.Length..]);
            }
            catch (FormatException)
            {
                throw new ServiceException(400, "Invalid image.");
            }

            var valid = prefix.Contains("png") ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) : bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
            if (!valid || bytes.Length > 204800)
                throw new ServiceException(400, "Invalid image type or size.");
        }
        else if (photo.Length > 2000 || (photo.Length > 0 && (!Uri.TryCreate(photo, UriKind.Absolute, out var uri) || uri.Scheme != "https")))
            throw new ServiceException(400, "Photo must be a PNG/JPEG upload or HTTPS URL.");
        var names = input.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        await db.Collection("users").Document(CompanyService.Id(uid)).SetAsync(new Dictionary<string, object> { ["uid"] = uid, ["fullName"] = input.Name.Trim(), ["firstName"] = names[0], ["lastName"] = names.Length > 1 ? names[1] : "", ["photoURL"] = photo, ["updatedAt"] = FieldValue.ServerTimestamp }, SetOptions.MergeAll);
    }
}
