using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests for <c>GET /api/v1/plans</c>.
/// Verifies the endpoint returns a non-empty list of subscription plans.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("Plans")]
[AllureTag("Smoke", "Plans")]
public sealed class PlansSmokeTests : BaseApiTest
{
    private PlansClient _plans = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _plans = new PlansClient(Settings);
    }

    [TearDown]
    public override async Task TearDown()
    {
        _plans.Dispose();
        await base.TearDown();
    }

    [Test]
    [AllureStep("GET /api/v1/plans returns HTTP 200")]
    public async Task GetPlans_ReturnsHttp200()
    {
        var response = await _plans.GetPlansAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            because: "the plans endpoint must return HTTP 200 for authenticated requests");
    }

    [Test]
    [AllureStep("GET /api/v1/plans returns a non-empty list of plans")]
    public async Task GetPlans_ReturnsNonEmptyPlansList()
    {
        var response = await _plans.GetPlansAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Plans.Should().NotBeNullOrEmpty(
            because: "at least one subscription plan must exist in the system");
    }
}
