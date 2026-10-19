using System.Net;
using System.Text;
using Solvix.Sims.Services.Authentication;

namespace Solvix.Mobile.Auth.Tests;
public sealed class FirebaseAuthServiceTests
{
    private const string SignInJson = """
        {"localId":"uid-001","idToken":"id-first","refreshToken":"refresh-first","expiresIn":"3600"}
        """;
    private const string RefreshJson = """
        {"user_id":"uid-001","id_token":"id-refreshed","refresh_token":"refresh-rotated","expires_in":"3600"}
        """;
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    private static FirebaseAuthService Service(Handler handler, Store store, Clock? clock = null) => new(new HttpClient(handler), store, () => Task.FromResult(new FirebaseAuthOptions { ProjectId = "test-project", ApiKey = "public-test-key" }), clock);
    [Fact]
    public async Task SignIn_Remembered_StoresOnlyRefreshToken()
    {
        string? postedBody = null;
        var handler = new Handler(async (request, ct) =>
        {
            Assert.Equal("identitytoolkit.googleapis.com", request.RequestUri!.Host);
            Assert.Contains("accounts:signInWithPassword", request.RequestUri.AbsolutePath);
            postedBody = await request.Content!.ReadAsStringAsync(ct);
            return Json(SignInJson);
        });
        var store = new Store();
        var auth = Service(handler, store);
        await auth.SignInAsync("user@example.com", "test-only-password", true);
        Assert.Contains("test-only-password", postedBody!); // Sent to Firebase, never to our token store.
        Assert.Equal("refresh-first", store.Value);
        Assert.Equal("uid-001", auth.UserId);
        Assert.Equal("id-first", await auth.GetIdTokenAsync());
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task SignIn_NotRemembered_DoesNotPersistSession()
    {
        var store = new Store
        {
            Value = "old-user-refresh"
        };
        var auth = Service(new Handler((_, _) => Task.FromResult(Json(SignInJson))), store);
        await auth.SignInAsync("user@example.com", "test-password", false);
        Assert.Null(store.Value);
        Assert.Equal("id-first", await auth.GetIdTokenAsync());
    }

    [Fact]
    public async Task FailedSignIn_CannotReusePreviousSession()
    {
        var handler = new Handler((_, _) => Task.FromResult(Json(SignInJson)));
        var store = new Store();
        var auth = Service(handler, store);
        await auth.SignInAsync("user@example.com", "test-password", true);
        handler.Respond = (_, _) => Task.FromResult(Json("""
            {"error":{"message":"INVALID_LOGIN_CREDENTIALS"}}
            """, HttpStatusCode.BadRequest));
        var error = await Assert.ThrowsAsync<AuthException>(() => auth.SignInAsync("other@example.com", "wrong-password", true));
        Assert.Equal("INVALID_LOGIN_CREDENTIALS", error.Code);
        Assert.Null(auth.UserId);
        Assert.Null(store.Value);
        await Assert.ThrowsAsync<AuthException>(() => auth.GetIdTokenAsync());
    }

    [Fact]
    public async Task ConcurrentRequests_RefreshOnce_AndSaveRotatedRefreshToken()
    {
        var clock = new Clock();
        var store = new Store();
        var handler = new Handler((_, _) => Task.FromResult(Json(SignInJson)));
        var auth = Service(handler, store, clock);
        await auth.SignInAsync("user@example.com", "test-password", true);
        clock.Now = clock.Now.AddHours(1);
        handler.Respond = async (request, ct) =>
        {
            Assert.Equal("securetoken.googleapis.com", request.RequestUri!.Host);
            Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);
            var body = await request.Content.ReadAsStringAsync(ct);
            Assert.Contains("grant_type=refresh_token", body);
            Assert.Contains("refresh_token=refresh-first", body);
            return Json(RefreshJson);
        };
        var tokens = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => auth.GetIdTokenAsync()));
        Assert.All(tokens, token => Assert.Equal("id-refreshed", token));
        Assert.Equal("refresh-rotated", store.Value);
        Assert.Equal(2, handler.Count); // One initial sign-in and one refresh.
    }

    [Fact]
    public async Task Restore_RefreshesSavedSessionBeforeReturning()
    {
        var store = new Store
        {
            Value = "refresh-first"
        };
        var handler = new Handler((_, _) => Task.FromResult(Json(RefreshJson)));
        var auth = Service(handler, store);
        Assert.True(await auth.RestoreAsync());
        Assert.Equal("uid-001", auth.UserId);
        Assert.Equal("id-refreshed", await auth.GetIdTokenAsync());
        Assert.Equal("refresh-rotated", store.Value);
    }

    [Fact]
    public async Task Restore_RevokedRefreshToken_ClearsSession()
    {
        var store = new Store
        {
            Value = "revoked-refresh"
        };
        var handler = new Handler((_, _) => Task.FromResult(Json("""
            {"error":{"message":"TOKEN_EXPIRED"}}
            """, HttpStatusCode.BadRequest)));
        var auth = Service(handler, store);
        bool signedOut = false;
        auth.SignedOut += () => signedOut = true;
        await Assert.ThrowsAsync<AuthException>(() => auth.RestoreAsync());
        Assert.True(signedOut);
        Assert.Null(store.Value);
        Assert.Null(auth.UserId);
    }

    [Fact]
    public async Task SignOut_RemovesTokens_AndPreventsFurtherAuthenticatedCalls()
    {
        var store = new Store();
        var auth = Service(new Handler((_, _) => Task.FromResult(Json(SignInJson))), store);
        await auth.SignInAsync("user@example.com", "test-password", true);
        await auth.SignOutAsync();
        Assert.Null(store.Value);
        Assert.Null(auth.UserId);
        var error = await Assert.ThrowsAsync<AuthException>(() => auth.GetIdTokenAsync());
        Assert.Equal("SIGN_IN_REQUIRED", error.Code);
    }

    [Fact]
    public async Task Register_UsesFirebaseSignup_WithoutPersistingSession()
    {
        var handler = new Handler((request, _) =>
        {
            Assert.Contains("accounts:signUp", request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(SignInJson));
        });
        var store = new Store();
        var auth = Service(handler, store);
        await auth.RegisterAsync("user@example.com", "test-password");
        Assert.Null(store.Value);
        Assert.Equal("uid-001", auth.UserId);
    }

    [Fact]
    public async Task Restore_EmptyStorage_ReturnsFalseWithoutNetwork()
    {
        var handler = new Handler((_, _) => throw new InvalidOperationException("Unexpected network request."));
        Assert.False(await Service(handler, new Store()).RestoreAsync());
        Assert.Equal(0, handler.Count);
    }

    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond;
        public int Count;
        public Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) => Respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Count);
            return Respond(request, ct);
        }
    }

    private sealed class Store : IRefreshTokenStore
    {
        public string? Value;
        public Task<string?> ReadAsync() => Task.FromResult(Value);
        public Task WriteAsync(string token)
        {
            Value = token;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Value = null;
            return Task.CompletedTask;
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
