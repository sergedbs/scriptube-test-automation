using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests verifying that the API correctly rejects requests with missing or invalid authentication.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("Authentication")]
[AllureTag("Smoke", "Auth", "Negative", "Security")]
public sealed class AuthNegativeSmokeTests : BaseTest
{
    /// <summary>
    /// Creates a <see cref="TestSettings"/> clone with the given API key overridden.
    /// </summary>
    private TestSettings WithApiKey(string apiKey) => Settings with { ApiKey = apiKey };

    [Test]
    [AllureStep("GET /api/v1/credits/balance with no API key returns HTTP 401")]
    public async Task GetBalance_WithNoApiKey_Returns401()
    {
        using var client = new CreditsClient(WithApiKey(string.Empty));

        var response = await client.GetBalanceAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            because: "requests without an API key must be rejected with HTTP 401");
    }

    [Test]
    [AllureStep("GET /api/v1/credits/balance with invalid API key returns HTTP 401")]
    public async Task GetBalance_WithInvalidApiKey_Returns401()
    {
        using var client = new CreditsClient(WithApiKey("invalid-api-key-00000000"));

        var response = await client.GetBalanceAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            because: "requests with an invalid API key must be rejected with HTTP 401");
    }

    [Test]
    [AllureStep("GET /api/v1/user with no API key returns HTTP 401")]
    public async Task GetUser_WithNoApiKey_Returns401()
    {
        using var client = new UserClient(WithApiKey(string.Empty));

        var response = await client.GetUserAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            because: "the user endpoint must reject unauthenticated requests with HTTP 401");
    }
}
