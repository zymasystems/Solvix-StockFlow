using Microsoft.AspNetCore.Mvc;
using Moq;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Controllers;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Tests.Support;
using Solvix.Web.ViewModels.Users;
namespace Solvix.Web.Tests.Controllers;
public sealed class UsersControllerTests
{
    [Fact]
    public async Task Edit_Get_LoadsSelectedUsersRoleAndStatus()
    {
        var user = TestData.Profile("staff", "Inactive");
        var service = new Mock<IUserService>(MockBehavior.Strict);
        service.Setup(x => x.GetAsync(user.Uid)).ReturnsAsync(user);
        var controller = new UsersController(service.Object);
        var result = Assert.IsType<ViewResult>(await controller.Edit(user.Uid));
        var model = Assert.IsType<UserEditViewModel>(result.Model);
        Assert.Equal("staff", model.Role);
        Assert.Equal("Inactive", model.Status);
        Assert.Equal(user.Uid, controller.ViewData["Id"]);
    }
    [Fact]
    public async Task Edit_Post_ApprovalCallsServiceAndRedirectsToList()
    {
        var model = new UserEditViewModel
        { FirstName = "New", LastName = "Staff", Company = "Test", Role = "staff", Status = "Active" };
        var service = new Mock<IUserService>(MockBehavior.Strict);
        service.Setup(x => x.UpdateAsync("pending-001", model)).ReturnsAsync(TestData.Profile());
        var controller = new UsersController(service.Object);
        var result = Assert.IsType<RedirectToActionResult>(await controller.Edit("pending-001", model));
        Assert.Equal("Index", result.ActionName);
        service.Verify(x => x.UpdateAsync("pending-001", model), Times.Once);
    }
    [Fact]
    public async Task Edit_Post_WhenModelInvalid_DoesNotCallService()
    {
        var service = new Mock<IUserService>(MockBehavior.Strict);
        var controller = new UsersController(service.Object);
        controller.ModelState.AddModelError("FirstName", "First name is required.");
        var model = new UserEditViewModel();
        var result = Assert.IsType<ViewResult>(await controller.Edit("pending-001", model));
        Assert.Same(model, result.Model);
        Assert.Equal("pending-001", controller.ViewData["Id"]);
        service.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task Edit_Post_WhenSelfDemotionConflicts_RedisplaysError()
    {
        var model = new UserEditViewModel
        { FirstName = "Test", LastName = "Admin", Role = "staff", Status = "Active" };
        var service = new Mock<IUserService>(MockBehavior.Strict);
        service.Setup(x => x.UpdateAsync("admin-001", model))
            .ThrowsAsync(new ServiceException(409, "You cannot remove your own administrator access."));
        var controller = new UsersController(service.Object);
        var result = Assert.IsType<ViewResult>(await controller.Edit("admin-001", model));
        Assert.Same(model, result.Model);
        Assert.Contains(controller.ModelState.Values.SelectMany(x => x.Errors),
            x => x.ErrorMessage == "You cannot remove your own administrator access.");
    }
}
