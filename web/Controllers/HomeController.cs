using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Solvix.Web.Controllers;
public sealed class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();
}
