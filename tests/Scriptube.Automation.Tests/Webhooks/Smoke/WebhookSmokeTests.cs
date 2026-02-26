using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Webhooks.Tests;

namespace Scriptube.Automation.Tests.Webhooks.Smoke;

/// <summary>
/// Smoke tests for the Scriptube webhook API.
/// Covers registration, available-events discovery, and SSRF protection.
/// </summary>
[TestFixture]
[NonParallelizable] // Shares the static WebhookReceiverManager / ReceivedRequestStore singleton.
[Category("Smoke")]
[Category("Webhook")]
[AllureSuite("Smoke")]
[AllureFeature("Webhooks")]
[AllureTag("Smoke", "Webhook")]
public sealed class WebhookSmokeTests : BaseWebhookTest
{
    private static readonly string TestSecret = WebhookTestData.SmokeSecret;
    private static readonly string BatchCompletedEvent = WebhookTestData.EventBatchCompleted;

    private readonly List<string> _webhookIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _webhookIdsToCleanup.Clear();
    }

    [TearDown]
    public override async Task TearDown()
    {
        foreach (var id in _webhookIdsToCleanup)
        {
            try { await Webhooks.DeleteAsync(id); }
            catch { /* best-effort cleanup */ }
        }

        await base.TearDown();
    }

    // -------------------------------------------------------------------------
    // Registration — positive
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("POST /api/webhooks/register with valid HTTPS URL returns HTTP 201 and a webhook ID")]
    public async Task RegisterWebhook_WithValidUrl_Returns201AndWebhookId()
    {
        SkipIfNoReceiverUrl();
        // Arrange
        var request = new WebhookRegisterRequest
        {
            Url = WebhookUrl!,
            Events = [BatchCompletedEvent],
            Secret = TestSecret,
        };

        // Act
        var response = await Webhooks.RegisterAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "registering a webhook with a valid HTTPS URL must return HTTP 201");
        response.Data.Should().NotBeNull();
        response.Data!.WebhookId.Should().NotBeNullOrWhiteSpace(
            because: "the response must include a non-empty webhook ID");
        response.Data.Status.Should().NotBeNullOrWhiteSpace();

        _webhookIdsToCleanup.Add(response.Data.WebhookId!);
    }

    // -------------------------------------------------------------------------
    // Available events
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("GET /api/webhooks/events/available returns HTTP 200 with a non-empty event list")]
    public async Task GetAvailableEvents_ReturnsHttp200WithNonEmptyList()
    {
        // Act
        var response = await Webhooks.GetAvailableEventsAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the available-events endpoint must always be accessible");
        response.Data.Should().NotBeNull();
        response.Data!.Events.Should().NotBeEmpty(
            because: "the API must advertise at least one subscribable event");
        response.Data.Descriptions.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // List webhooks
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("GET /api/webhooks after registering one webhook returns a list containing the new ID")]
    public async Task ListWebhooks_AfterRegister_ContainsRegisteredWebhook()
    {
        SkipIfNoReceiverUrl();
        // Arrange — register
        var registerResponse = await Webhooks.RegisterAsync(new WebhookRegisterRequest
        {
            Url = WebhookUrl!,
            Events = [BatchCompletedEvent],
            Secret = TestSecret,
        });
        registerResponse.Data.Should().NotBeNull();
        var webhookId = registerResponse.Data!.WebhookId!;
        _webhookIdsToCleanup.Add(webhookId);

        // Act — list
        var listResponse = await Webhooks.ListAsync();

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "GET /api/webhooks must return HTTP 200");
        listResponse.Data.Should().NotBeNull();
        listResponse.Data!.Count.Should().BeGreaterThanOrEqualTo(1,
            because: "the list must include the webhook we just registered");
        listResponse.Data.Webhooks
            .Should().Contain(w => w.WebhookId == webhookId,
                because: "the newly registered webhook must appear in the list");
    }

    // -------------------------------------------------------------------------
    // SSRF protection — negative
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("POST /api/webhooks/register with http://localhost/x is rejected")]
    public async Task RegisterWebhook_WithLocalhostUrl_IsRejectedWith4xx()
    {
        var response = await Webhooks.RegisterAsync(new WebhookRegisterRequest
        {
            Url = WebhookTestData.SsrfLocalhost,
            Events = [BatchCompletedEvent],
            Secret = TestSecret,
        });

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            because: "SSRF via localhost must be blocked with a 4xx response");
    }

    [Test]
    [AllureStep("POST /api/webhooks/register with http://192.168.1.1/x is rejected")]
    public async Task RegisterWebhook_WithPrivateRange192_IsRejectedWith4xx()
    {
        var response = await Webhooks.RegisterAsync(new WebhookRegisterRequest
        {
            Url = WebhookTestData.SsrfPrivate192,
            Events = [BatchCompletedEvent],
            Secret = TestSecret,
        });

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            because: "SSRF via 192.168.x.x private range must be blocked with a 4xx response");
    }

    [Test]
    [AllureStep("POST /api/webhooks/register with http://10.0.0.1/x is rejected")]
    public async Task RegisterWebhook_WithPrivateRange10_IsRejectedWith4xx()
    {
        var response = await Webhooks.RegisterAsync(new WebhookRegisterRequest
        {
            Url = WebhookTestData.SsrfPrivate10,
            Events = [BatchCompletedEvent],
            Secret = TestSecret,
        });

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            because: "SSRF via 10.x.x.x private range must be blocked with a 4xx response");
    }
}
