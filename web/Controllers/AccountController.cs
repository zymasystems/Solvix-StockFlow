using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Solvix.Web.Authentication;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Account;
namespace Solvix.Web.Controllers;
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController : Controller
{
    private readonly FirebaseTokenVerifier verifier;
    private readonly IAccountService accounts;
    public AccountController(FirebaseTokenVerifier verifier, IAccountService accounts)
    { this.verifier = verifier; this.accounts = accounts; }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new FirebaseLoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });

    // JS submits form data, including __RequestVerificationToken. Returns JSON, not HTML.
    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login([FromForm] FirebaseLoginViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        FirebaseToken token;
        try { token = await verifier.VerifyAsync(model.IdToken); }
        catch (FirebaseAuthException) { return Problem(statusCode: 401, title: "Sign-in failed", detail: "Sign in again to obtain a valid Firebase ID token."); }

        if (!RecentlyAuthenticated(token))
            return Problem(statusCode: 401, title: "Fresh sign-in required", detail: "Sign in again before starting a website session.");

        var principal = verifier.CreatePrincipal(token, Policies.WebCookieScheme);
        var previous = HttpContext.User;
        ProfileResponse profile;
        try
        {
            // Populate the CURRENT request identity before calling the profile service.
            HttpContext.User = principal;
            profile = await accounts.GetProfileAsync();
        }
        finally { HttpContext.User = previous; }

        await HttpContext.SignInAsync(Policies.WebCookieScheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
        return Json(new {
            success = true,
            role = profile.Role,
            redirectUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : Url.Action("Index", "Dashboard")
        });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterProfileViewModel());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Register([FromForm] RegisterProfileViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        FirebaseToken token;
        try { token = await verifier.VerifyAsync(model.IdToken); }
        catch (FirebaseAuthException) { return Problem(statusCode: 401, title: "Invalid Firebase identity"); }
        if (!RecentlyAuthenticated(token)) return Problem(statusCode: 401, title: "Fresh sign-in required");
        var previous = HttpContext.User;
        try
        {
            HttpContext.User = verifier.CreatePrincipal(token, Policies.WebCookieScheme);
            var profile = await accounts.CreateProfileAsync(new CreateProfileRequest {
                FirstName = model.FirstName, LastName = model.LastName,
                Company = model.Company, AcceptTerms = model.AcceptTerms
            });
            // No website cookie is issued to an unapproved account.
            return StatusCode(201, new { success = true, status = profile.Status,
                message = "Your profile has been created and is awaiting administrator approval." });
        }
        finally { HttpContext.User = previous; }
    }

    // Allow an expired/inactive session to clear its cookie too; CSRF protection still applies.
    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(Policies.WebCookieScheme);
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Login), "Account") });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    private static bool RecentlyAuthenticated(FirebaseToken token)
    {
        if (!token.Claims.TryGetValue("auth_time", out var raw) || raw is null) return false;
        if (!long.TryParse(Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture), out var seconds)) return false;
        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - seconds;
        return age >= -60 && age <= 300;
    }
}
