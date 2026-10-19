using Moq;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;
namespace Solvix.Web.Tests.Support;
internal static class TestData
{
    public static ProfileResponse Profile(string role = "staff", string status = "Active") => new()
    {
        Uid = "test-user-001", Email = "tester@example.com", FirstName = "Test",
        LastName = "User", DisplayName = "Test User", Company = "Test business",
        Role = role, Status = status
    };
    public static Mock<ICurrentUserService> CurrentUser(ProfileResponse? actor = null)
    {
        actor ??= Profile();
        var mock = new Mock<ICurrentUserService>(MockBehavior.Strict);
        mock.Setup(x => x.RequireAsync(false)).ReturnsAsync(actor);
        mock.Setup(x => x.RequireAsync(true)).ReturnsAsync(actor);
        return mock;
    }
    public static CreateProductRequest Product(decimal price = 12.50m) => new()
    {
        Name = "Test notebook", Sku = "NB-001", Category = "Accessories",
        Price = price, OpeningStock = 10, MinStock = 2
    };
}
