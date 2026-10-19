using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Tests.Support;
namespace Solvix.Web.Tests.Services;
public sealed class CurrentUserServiceTests
{
    private readonly Mock<IUserRepository> users = new(MockBehavior.Strict);
    private CurrentUserService Service(ClaimsPrincipal? principal = null)
    {
        var context = new DefaultHttpContext();
        if (principal is not null) context.User = principal;
        return new CurrentUserService(new HttpContextAccessor { HttpContext = context }, users.Object);
    }
    private static ClaimsPrincipal SignedIn(string? uid = "test-user-001") => new(
        new ClaimsIdentity(uid is null ? Array.Empty<Claim>() :
            new[] { new Claim(ClaimTypes.NameIdentifier, uid) }, "UnitTest"));

    [Fact]
    public void GetUid_WhenAnonymous_RejectsWithoutRepositoryAccess()
    {
        var error = Assert.Throws<ServiceException>(() => Service().GetUid());
        Assert.Equal(401, error.StatusCode);
        users.VerifyNoOtherCalls();
    }
    [Fact]
    public void GetUid_WhenHttpContextMissing_Rejects()
    {
        var service = new CurrentUserService(new HttpContextAccessor(), users.Object);
        Assert.Equal(401, Assert.Throws<ServiceException>(() => service.GetUid()).StatusCode);
    }
    [Fact]
    public void GetUid_WhenIdentifierMissing_Rejects()
    {
        Assert.Equal(401, Assert.Throws<ServiceException>(() => Service(SignedIn(null)).GetUid()).StatusCode);
    }
    [Fact]
    public void GetUid_WhenAuthenticated_ReturnsVerifiedIdentifier()
    {
        Assert.Equal("test-user-001", Service(SignedIn()).GetUid());
    }
    [Fact]
    public async Task RequireAsync_WhenProfileMissing_Rejects()
    {
        users.Setup(x => x.FindAsync("test-user-001")).ReturnsAsync((ProfileResponse?)null);
        var error = await Assert.ThrowsAsync<ServiceException>(() => Service(SignedIn()).RequireAsync());
        Assert.Equal(403, error.StatusCode);
    }
    [Theory]
    [InlineData("staff", "Inactive")]
    [InlineData("admin", "Inactive")]
    [InlineData("owner", "Active")]
    [InlineData("Admin", "Active")]
    [InlineData("staff", "active")]
    public async Task RequireAsync_WhenProfileNotApproved_Rejects(string role, string status)
    {
        users.Setup(x => x.FindAsync("test-user-001")).ReturnsAsync(TestData.Profile(role, status));
        var error = await Assert.ThrowsAsync<ServiceException>(() => Service(SignedIn()).RequireAsync());
        Assert.Equal(403, error.StatusCode);
    }
    [Theory]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task RequireAsync_WhenActive_ReturnsStoredProfile(string role)
    {
        var profile = TestData.Profile(role);
        users.Setup(x => x.FindAsync(profile.Uid)).ReturnsAsync(profile);
        Assert.Same(profile, await Service(SignedIn()).RequireAsync());
    }
    [Fact]
    public async Task RequireAsync_WhenStaffRequestsAdminAccess_Rejects()
    {
        users.Setup(x => x.FindAsync("test-user-001")).ReturnsAsync(TestData.Profile());
        var error = await Assert.ThrowsAsync<ServiceException>(() => Service(SignedIn()).RequireAsync(true));
        Assert.Equal(403, error.StatusCode);
    }
    [Fact]
    public async Task RequireAsync_WhenActiveAdminRequestsAdminAccess_Allows()
    {
        var profile = TestData.Profile("admin");
        users.Setup(x => x.FindAsync(profile.Uid)).ReturnsAsync(profile);
        Assert.Same(profile, await Service(SignedIn()).RequireAsync(true));
    }
    [Fact]
    public async Task RequireAsync_WhenApprovalRevoked_DoesNotReuseOldProfile()
    {
        users.SetupSequence(x => x.FindAsync("test-user-001"))
            .ReturnsAsync(TestData.Profile("admin"))
            .ReturnsAsync(TestData.Profile("admin", "Inactive"));
        var service = Service(SignedIn());
        await service.RequireAsync(true);
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.RequireAsync(true));
        Assert.Equal(403, error.StatusCode);
        users.Verify(x => x.FindAsync("test-user-001"), Times.Exactly(2));
    }
}
