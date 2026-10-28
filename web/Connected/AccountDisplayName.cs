using Solvix.Web.Contracts.Responses;

namespace Solvix.Web.Connected;

internal static class AccountDisplayName
{
    public static string From(ProfileResponse profile)
    {
        // UID is an identity key, never a greeting or a person's name.
        var candidates = new[] { profile.DisplayName, $"{profile.FirstName} {profile.LastName}", profile.Email };
        return candidates.Select(value => value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value != profile.Uid)
            ?? "Team member";
    }
}
