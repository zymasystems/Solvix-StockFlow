using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Solvix.Web.Authentication;

public sealed class FirebaseBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "FirebaseBearer";
    private readonly FirebaseTokenVerifier verifier;

    public FirebaseBearerHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        FirebaseTokenVerifier verifier)
        : base(options, logger, encoder)
    {
        this.verifier = verifier;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var values))
            return AuthenticateResult.NoResult();

        if (values.Count != 1 ||
            !AuthenticationHeaderValue.TryParse(values[0], out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.Fail("A single Bearer ID token is required.");
        }

        try
        {
            var token = await verifier.VerifyAsync(header.Parameter);
            var principal = verifier.CreatePrincipal(token, Scheme.Name);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (FirebaseAuthException)
        {
            // Never expose the token or provider exception text to the caller/logs.
            return AuthenticateResult.Fail("The Firebase ID token could not be verified.");
        }
        // Unexpected infrastructure exceptions propagate to central exception handling.
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        Response.Headers["Cache-Control"] = "no-store";
        return Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication required",
            Detail = "Provide a valid Firebase ID token in the Authorization header."
        }, options: null, contentType: "application/problem+json",
           cancellationToken: Context.RequestAborted);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.Headers["Cache-Control"] = "no-store";
        return Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access denied",
            Detail = "Your account does not have permission for this operation."
        }, options: null, contentType: "application/problem+json",
           cancellationToken: Context.RequestAborted);
    }
}
