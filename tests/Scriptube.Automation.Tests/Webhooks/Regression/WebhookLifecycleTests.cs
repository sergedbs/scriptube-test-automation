using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Utilities;
using Scriptube.Automation.Webhooks.Tests;

namespace Scriptube.Automation.Tests.Webhooks.Regression;

/// <summary>
/// Regression tests for the full webhook lifecycle: register, get, delete, test events,
/// delivery logs, retry, HMAC signature verification, and batch-completion delivery.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("Webhook")]
[AllureSuite("Regression")]
[AllureFeature("Webhooks")]
[AllureTag("Regression", "Webhook")]
public sealed class WebhookLifecycleTests : BaseWebhookTest
{
    private static readonly string TestSecret = WebhookTestData.RegressionSecret;
    private static readonly string BatchCompletedEvent = WebhookTestData.EventBatchCompleted;

    private readonly List<string> _webhookIdsToCleanup = [];
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _webhookIdsToCleanup.Clear();
        _batchIdsToCleanup.Clear();
    }

    [TearDown]
    public override async Task TearDown()
    {
        foreach (var id in _webhookIdsToCleanup)
        {
            try { await Webhooks.DeleteAsync(id); }
            catch { /* best-effort cleanup */ }
        }

        foreach (var batchId in _batchIdsToCleanup)
        {
            try { await Transcripts.DeleteAsync(batchId); }
            catch { /* best-effort cleanup — DELETE may return 405 */ }
        }

        await base.TearDown();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers a test webhook with the active receiver URL, tracks it for cleanup,
    /// and returns its ID.
    /// </summary>
    /// <param name="secret">
    /// Signing secret to register the webhook with. Defaults to <see cref="TestSecret"/> when
    /// <see langword="null"/>.
    /// </param>
    private async Task<string> RegisterTestWebhookAsync(string? secret = null)
    {
        SkipIfNoReceiverUrl();
        var response = await Webhooks.RegisterAsync(new WebhookRegisterRequest
        {
            Url = WebhookUrl!,
            Events = [BatchCompletedEvent],
            Secret = secret ?? TestSecret,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data!.WebhookId.Should().NotBeNullOrWhiteSpace();
        var id = response.Data.WebhookId!;
        _webhookIdsToCleanup.Add(id);
        return id;
    }

    /// <summary>
    /// Polls GET /api/webhooks/{id}/logs until at least one delivery entry appears,
    /// or throws <see cref="TimeoutException"/> when <paramref name="timeout"/> elapses.
    /// </summary>
    private async Task<DeliveryLogsResponse> PollUntilDeliveryAsync(
        string webhookId,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await Webhooks.GetLogsAsync(webhookId);
            ((int)response.StatusCode).Should().BeInRange(200, 299,
                because: "GET /api/webhooks/{id}/logs must return a success status");
            if (response.Data?.Deliveries?.Count > 0)
            {
                return response.Data;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        throw new TimeoutException(
            $"No webhook delivery appeared in logs for webhook '{webhookId}' " +
            $"within {timeout.TotalSeconds:F0} second(s).");
    }

    // -------------------------------------------------------------------------
    // Get + Delete lifecycle
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("GET /api/webhooks/{id} after registering returns matching url and events")]
    public async Task GetWebhook_AfterRegister_ReturnsMatchingDetails()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();

        // Act
        var response = await Webhooks.GetAsync(webhookId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "GET for a registered webhook must return HTTP 200");
        response.Data.Should().NotBeNull();
        response.Data!.WebhookId.Should().Be(webhookId);
        response.Data.Url.Should().Be(WebhookUrl,
            because: "the returned URL must match the registered value");
        response.Data.Events.Should().Contain(BatchCompletedEvent,
            because: "the subscribed event must be reflected in the response");
        response.Data.IsActive.Should().BeTrue(
            because: "a freshly registered webhook must be active");
    }

    [Test]
    [AllureStep("DELETE /api/webhooks/{id} returns HTTP 200 and subsequent GET fails")]
    public async Task DeleteWebhook_Returns200_AndSubsequentGetFails()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();

        // Act — delete
        var deleteResponse = await Webhooks.DeleteAsync(webhookId);

        // Assert — delete succeeded
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "deleting an existing webhook must return HTTP 200");

        // Remove from cleanup list since we already deleted it
        _webhookIdsToCleanup.Remove(webhookId);

        // Assert — gone
        var getResponse = await Webhooks.GetAsync(webhookId);
        ((int)getResponse.StatusCode).Should().BeInRange(400, 499,
            because: "GET for a deleted webhook must return a 4xx error");
    }

    // -------------------------------------------------------------------------
    // Test event + logs + retry
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("POST /api/webhooks/{id}/test returns HTTP 200 with a delivery ID")]
    public async Task TriggerTestEvent_Returns200WithDeliveryId()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();

        // Act
        var response = await Webhooks.TriggerTestAsync(webhookId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "triggering a test event must return HTTP 200");
        response.Data.Should().NotBeNull();
        response.Data!.DeliveryId.Should().NotBeNullOrWhiteSpace(
            because: "the test event response must include a delivery ID");
        response.Data.Status.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [AllureStep("GET /api/webhooks/{id}/logs after triggering a test event contains a delivery entry")]
    public async Task GetLogs_AfterTriggerTest_ContainsDeliveryEntry()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();
        await Webhooks.TriggerTestAsync(webhookId);

        // Poll until the delivery is recorded — more resilient than a fixed delay.
        var logs = await PollUntilDeliveryAsync(webhookId,
            TimeSpan.FromSeconds(Settings.Timeouts.WebhookDispatchWaitSeconds));

        // Assert
        logs.Deliveries.Should().NotBeEmpty(
            because: "triggering a test event must produce at least one delivery log entry");
        logs.Deliveries[0].DeliveryId.Should().NotBeNullOrWhiteSpace();
        logs.Deliveries[0].Event.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [AllureStep("POST /api/webhooks/{id}/retry with a valid delivery ID returns HTTP 200")]
    public async Task RetryDelivery_WithValidDeliveryId_Returns200()
    {
        // Arrange — trigger test event and get the resulting delivery ID from logs
        var webhookId = await RegisterTestWebhookAsync();
        await Webhooks.TriggerTestAsync(webhookId);

        var logs = await PollUntilDeliveryAsync(webhookId,
            TimeSpan.FromSeconds(Settings.Timeouts.WebhookDispatchWaitSeconds));
        logs.Deliveries.Should().NotBeEmpty(
            because: "a delivery log entry is required as a prerequisite for this test");
        var deliveryId = logs.Deliveries[0].DeliveryId;

        // Act
        var retryResponse = await Webhooks.RetryDeliveryAsync(webhookId, deliveryId);

        // Assert
        // The API only retries *failed* deliveries. When the receiver returns HTTP 200, the
        // delivery is recorded as successful and the retry endpoint returns 404 (nothing
        // to retry). Both outcomes are acceptable — 200 means re-queued, 404 means
        // the original delivery already succeeded.
        ((int)retryResponse.StatusCode).Should().BeOneOf([200, 404],
            because: "retry returns HTTP 200 when re-queued, or 404 when the delivery already succeeded");
    }

    // -------------------------------------------------------------------------
    // Batch completion → delivery
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("Submit batch (use_byok=false) → complete → GET logs contains batch.completed entry")]
    public async Task BatchComplete_WithoutByok_DeliveryLogContainsBatchCompletedEvent()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();

        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submitResponse = await Transcripts.SubmitAsync(request);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "batch submission must return HTTP 202");

        var batchId = submitResponse.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        // Act — poll until batch completes
        var batch = await Transcripts.PollUntilCompleteAsync(batchId);
        batch.Status.Should().BeOneOf(["completed", "failed"],
            because: "polling must end in a terminal status");

        // Poll until the webhook delivery is recorded in logs.
        var logs = await PollUntilDeliveryAsync(webhookId,
            TimeSpan.FromSeconds(Settings.Timeouts.WebhookDispatchWaitSeconds));

        // Assert
        logs.Deliveries.Should().NotBeEmpty(
            because: "batch completion must trigger at least one webhook delivery");

        logs.Deliveries
            .Should().Contain(d => d.Event.Contains("batch"),
                because: "at least one delivery must correspond to the batch event");

        logs.Deliveries
            .Should().Contain(d => d.ResponseCode != null,
                because: "a delivery attempt response code must be recorded");
    }

    [Test]
    [AllureStep("Submit batch (use_byok=true) → complete → GET logs contains a delivery entry")]
    public async Task BatchComplete_WithByok_DeliveryLogCreated()
    {
        // Arrange
        var webhookId = await RegisterTestWebhookAsync();

        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .WithByok()
            .Build();

        var submitResponse = await Transcripts.SubmitAsync(request);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "batch submission with use_byok=true must return HTTP 202");

        var batchId = submitResponse.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        // Act — poll + wait for delivery
        await Transcripts.PollUntilCompleteAsync(batchId);

        var logs = await PollUntilDeliveryAsync(webhookId,
            TimeSpan.FromSeconds(Settings.Timeouts.WebhookDispatchWaitSeconds));

        // Assert
        logs.Deliveries.Should().NotBeEmpty(
            because: "batch completion with use_byok=true must also trigger a webhook delivery");
    }

    // -------------------------------------------------------------------------
    // HMAC signature verification (requires local receiver + ngrok)
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("HMAC-SHA256 signature in X-Scriptube-Signature matches locally computed value")]
    public async Task HmacSignature_MatchesLocallyComputedValue()
    {
        if (!HasLocalReceiver)
        {
            Assert.Ignore(
                "This test requires the local HttpListener receiver and an ngrok tunnel. " +
                $"Leave WEBHOOK_RECEIVER_URL empty in .env and run 'ngrok http {ReceiverPort}' before executing.");
        }

        // Arrange — register directly with the HMAC-specific secret
        var hmacSecret = WebhookTestData.HmacVerificationSecret;
        var webhookId = await RegisterTestWebhookAsync(hmacSecret);

        // Act — trigger test event; WaitForRequestAsync polls until the delivery arrives
        ReceiverStore!.Clear();
        await Webhooks.TriggerTestAsync(webhookId);

        var received = await ReceiverStore.WaitForRequestAsync(
            timeout: TimeSpan.FromSeconds(Settings.Timeouts.WebhookDeliveryTimeoutSeconds));

        // Assert
        received.Headers.TryGetValue("x-scriptube-signature", out var signature)
            .Should().BeTrue(
                because: "Scriptube must include the X-Scriptube-Signature header on every delivery");

        // Scriptube signs json.dumps(payload, sort_keys=True) — canonicalise before verifying.
        var canonical = HmacVerifier.CanonicalizeJson(received.Body);
        var expected = HmacVerifier.Compute(hmacSecret, canonical);

        HmacVerifier.Verify(hmacSecret, canonical, signature!)
            .Should().BeTrue(
                because: $"HMAC-SHA256 of the canonicalised payload must equal " +
                          $"the X-Scriptube-Signature header value. " +
                          $"Computed={expected}, Header={signature}");
    }
}
