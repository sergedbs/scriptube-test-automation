using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests for <c>GET /api/v1/user</c>.
/// Verifies the endpoint is reachable and returns valid user profile data.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("User")]
[AllureTag("Smoke", "User")]
public sealed class UserSmokeTests : BaseApiTest
{
    private UserClient _user = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _user = new UserClient(Settings);
    }

    [TearDown]
    public override void TearDown()
    {
        _user.Dispose();
        base.TearDown();
    }

    [Test]
    [AllureStep("GET /api/v1/user returns HTTP 200")]
    public async Task GetUser_ReturnsHttp200()
    {
        var response = await _user.GetUserAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            because: "the user endpoint must return HTTP 200 for authenticated requests");
    }

    [Test]
    [AllureStep("GET /api/v1/user returns a non-empty email address")]
    public async Task GetUser_ReturnsEmail()
    {
        var response = await _user.GetUserAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Email.Should().NotBeNullOrWhiteSpace(
            because: "the authenticated user must have an email address");
        response.Data.Email.Should().Contain("@",
            because: "email must be a valid email format");
    }

    [Test]
    [AllureStep("GET /api/v1/user returns plan details")]
    public async Task GetUser_ReturnsPlanDetails()
    {
        var response = await _user.GetUserAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Plan.Should().NotBeNullOrWhiteSpace(
            because: "the user profile must include the active plan name");
    }

    [Test]
    [AllureStep("GET /api/v1/user returns a non-empty user ID")]
    public async Task GetUser_ReturnsUserId()
    {
        var response = await _user.GetUserAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.UserId.Should().NotBeNullOrWhiteSpace(
            because: "user ID must be present in the user profile");
    }
}
