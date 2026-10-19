using System.Net;
using System.Text;
using Solvix.Sims.Services;
using Solvix.Sims.Services.Authentication;

namespace Solvix.Mobile.Auth.Tests;
public sealed class ApiServiceTests
{
    private sealed class Auth : IFirebaseAuthService
    {
        public string? UserId => "u";

        public event Action? SignedOut;
        public int Tokens;
        public int Logouts;
        public Task<string> GetIdTokenAsync(CancellationToken ct = default) => Task.FromResult("token-" + (++Tokens));
        public Task SignOutAsync()
        {
            Logouts++;
            SignedOut?.Invoke();
            return Task.CompletedTask;
        }

        public Task SignInAsync(string e, string p, bool r, CancellationToken ct = default) => Task.CompletedTask;
        public Task RegisterAsync(string e, string p, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> RestoreAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task SendPasswordResetAsync(string e, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequestEmailChangeAsync(string e, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Online : IConnectionStatus
    {
        public bool IsConnected => true;
    }

    private sealed class Handler(Func<HttpRequestMessage, int, HttpResponseMessage> action) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(action(r, ++Calls));
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) => new(code)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    private static ApiService Client(Auth auth, Handler h) => new(new HttpClient(h) { BaseAddress = new Uri("https://example.test/") }, auth, new Online());
    [Fact]
    public async Task EveryRequestGetsCurrentToken()
    {
        var a = new Auth();
        var h = new Handler((r, n) =>
        {
            Assert.Equal("Bearer", r.Headers.Authorization!.Scheme);
            Assert.Equal("token-" + n, r.Headers.Authorization.Parameter);
            return Json(HttpStatusCode.OK, "[]");
        });
        var c = Client(a, h);
        await c.GetAsync<string[]>("api/items");
        await c.GetAsync<string[]>("api/items");
        Assert.Equal(2, a.Tokens);
    }

    [Fact]
    public async Task GetRetriesTransientFailure()
    {
        var h = new Handler((r, n) => Json(n == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK, "[]"));
        await Client(new(), h).GetAsync<string[]>("api/items");
        Assert.Equal(2, h.Calls);
    }

    [Fact]
    public async Task MutationsDoNotAutomaticallyRetry()
    {
        var h = new Handler((r, n) => Json(HttpStatusCode.ServiceUnavailable, "{}"));
        await Assert.ThrowsAsync<ApiException>(() => Client(new(), h).PostAsync<object, object>("api/items", new { }));
        Assert.Equal(1, h.Calls);
    }

    [Fact]
    public async Task UnauthorizedClearsAuthentication()
    {
        var a = new Auth();
        var h = new Handler((r, n) => Json(HttpStatusCode.Unauthorized, "{}"));
        await Assert.ThrowsAsync<AuthException>(() => Client(a, h).GetAsync<object>("api/items"));
        Assert.Equal(1, a.Logouts);
    }

    [Fact]
    public async Task ForbiddenDoesNotSignOutAndPreservesError()
    {
        var a = new Auth();
        var h = new Handler((r, n) => Json(HttpStatusCode.Forbidden, "{\"detail\":\"Warehouse access denied.\"}"));
        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(a, h).GetAsync<object>("api/items"));
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("Warehouse access denied.", ex.Detail);
        Assert.Equal(0, a.Logouts);
    }

    [Fact]
    public async Task TokenCannotBeSentToAnotherHost()
    {
        var a = new Auth();
        var h = new Handler((r, n) => throw new Exception("Must not send"));
        await Assert.ThrowsAsync<ArgumentException>(() => Client(a, h).GetAsync<object>("https://evil.test/api/items"));
        Assert.Equal(0, a.Tokens);
    }
}
