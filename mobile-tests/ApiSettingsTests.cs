using System.Text;
using Solvix.Sims.Services;

namespace Solvix.Mobile.Auth.Tests;
public sealed class ApiSettingsTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"Solvix\":{\"Api\":{\"BaseUrl\":42}}}")]
    [InlineData("{\"Solvix\":{\"Api\":{\"BaseUrl\":\"http://localhost:5118\"}}}")]
    [InlineData("{\"Solvix\":{\"Api\":{\"BaseUrl\":\"https://YOUR_API_HOST/\"}}}")]
    [InlineData("{\"Solvix\":{\"Api\":{\"BaseUrl\":\"https://user:password@example.test/\"}}}")]
    [InlineData("{\"Solvix\":{\"Api\":{\"BaseUrl\":\"https://example.test/?token=secret\"}}}")]
    [InlineData("{broken")]
    public async Task BadConfigurationReturnsActionableError(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var error = await Assert.ThrowsAsync<ApiException>(() => ApiSettings.LoadAsync(stream));
        Assert.Contains("appsettings.json", error.Message);
        Assert.DoesNotContain("password", error.Message);
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task ValidAddressPreservesBackendSubpath()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Solvix\":{\"Api\":{\"BaseUrl\":\"https://example.test/solvix\"}}}"));
        var address = await ApiSettings.LoadAsync(stream);
        Assert.Equal("https://example.test/solvix/", address.AbsoluteUri);
    }
}
