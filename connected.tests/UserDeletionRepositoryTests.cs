using Google.Api.Gax;
using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Repositories;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Users;

namespace Solvix.Connected.Tests;

public sealed class UserDeletionRepositoryTests
{
    [EmulatorFact]
    public async Task PendingDeletionIsInactiveCannotBeReactivatedAndCleanupIsIdempotent()
    {
        var db = new FirestoreDbBuilder
        {
            ProjectId = "demo-delete-" + Guid.NewGuid().ToString("N")[..12],
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
        var users = new UserRepository(db);
        await users.CreateAsync("target", "target@example.test", new CreateProfileRequest
        {
            FirstName = "Target", LastName = "User", Company = "Solvix", AcceptTerms = true
        });
        await users.UpdateAdminAsync("target", new UserEditViewModel { FirstName = "Target", LastName = "User", Role = "staff", Status = "Active" });
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => users.DeleteAsync("target"))).StatusCode);
        await users.PrepareDeletionAsync("target");
        Assert.Equal("Inactive", (await users.FindAsync("target"))!.Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => users.UpdateAdminAsync("target", new UserEditViewModel { FirstName = "Target", LastName = "User", Role = "admin", Status = "Active" }))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<ServiceException>(() => users.UpdateNamesAsync("target", "Changed", "User"))).StatusCode);
        Assert.True((await db.Collection("users").Document("target").GetSnapshotAsync()).ContainsField("deletionRequestedAt"));
        await users.DeleteAsync("target");
        Assert.Null(await users.FindAsync("target"));
        await users.DeleteAsync("target");
    }
}
