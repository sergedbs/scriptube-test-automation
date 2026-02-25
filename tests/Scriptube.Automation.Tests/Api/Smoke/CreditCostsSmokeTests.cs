using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests for <c>GET /api/v1/credits/costs</c>.
/// Verifies the endpoint returns the credit cost table with at least one entry.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("Credits")]
[AllureTag("Smoke", "Credits", "Costs")]
[Ignore("GET /api/v1/credits/costs is not present in the public OpenAPI spec — endpoint does not exist.")]
public sealed class CreditCostsSmokeTests : BaseApiTest
{
    private CreditsClient _credits = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _credits = new CreditsClient(Settings);
    }

    [TearDown]
    public override void TearDown()
    {
        _credits.Dispose();
        base.TearDown();
    }

    [Test]
    [AllureStep("GET /api/v1/credits/costs returns HTTP 200")]
    public async Task GetCosts_ReturnsHttp200()
    {
        var response = await _credits.GetCostsAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            because: "the credit costs endpoint must return HTTP 200 for authenticated requests");
    }

    [Test]
    [AllureStep("GET /api/v1/credits/costs returns at least one cost table entry")]
    public async Task GetCosts_ReturnsCostTableEntries()
    {
        var response = await _credits.GetCostsAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Costs.Should().NotBeNullOrEmpty(
            because: "the cost table must contain at least one processing path entry");
    }
}
