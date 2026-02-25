using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests for <c>GET /api/v1/credits/balance</c>.
/// Verifies the endpoint is reachable and returns a valid numeric balance.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("Credits")]
[AllureTag("Smoke", "Credits", "Balance")]
public sealed class CreditsBalanceSmokeTests : BaseApiTest
{
    private CreditsClient _credits = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _credits = new CreditsClient(Settings);
    }

    [TearDown]
    public override async Task TearDown()
    {
        _credits.Dispose();
        await base.TearDown();
    }

    [Test]
    [AllureStep("GET /api/v1/credits/balance returns HTTP 200")]
    public async Task GetBalance_ReturnsHttp200()
    {
        var response = await _credits.GetBalanceAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            because: "the balance endpoint must return HTTP 200 for authenticated requests");
    }

    [Test]
    [AllureStep("GET /api/v1/credits/balance returns a non-negative numeric balance")]
    public async Task GetBalance_ReturnsNumericBalance()
    {
        var response = await _credits.GetBalanceAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.CreditsBalance.Should().BeGreaterThanOrEqualTo(0,
            because: "credits balance must be a non-negative integer");
    }

    [Test]
    [AllureStep("GET /api/v1/credits/balance returns a non-empty plan name")]
    public async Task GetBalance_ReturnsPlanName()
    {
        var response = await _credits.GetBalanceAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Plan.Should().NotBeNullOrWhiteSpace(
            because: "the plan field must be present in the balance response");
    }
}
