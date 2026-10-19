using System.Security.Claims;
using FirebaseAdmin.Auth;

namespace Solvix.Web.Authentication;

public sealed class FirebaseTokenVerifier
{
    private readonly FirebaseAuth auth;

    public FirebaseTokenVerifier(FirebaseAuth auth)
    {
        this.auth = auth;
    }

    // FirebaseAuth is configured by AddSolvixServices using your project ID and ADC.
    // The Admin SDK checks signature, issuer, audience, expiry, revocation and disabled users.
    public Task<FirebaseToken> VerifyAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("A Firebase ID token is required.", nameof(idToken));

        return auth.VerifyIdTokenAsync(idToken, checkRevoked: true);
    }

    // Call only AFTER VerifyAsync succeeds. Do not construct a FirebaseToken from client JSON.
    public ClaimsPrincipal CreatePrincipal(FirebaseToken verifiedToken, string scheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, verifiedToken.Uid)
        };

        // Deliberately exclude role/admin claims: Firestore is the application's
        // current authority for role and approval status.
        var identity = new ClaimsIdentity(claims, scheme);
        return new ClaimsPrincipal(identity);
    }
}
