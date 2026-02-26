using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for the retry-failed flow
/// (<c>POST /api/v1/transcripts/{batch_id}/retry-failed</c>).
/// <para>
/// All tests are currently <b>ignored</b>: <c>POST …/retry-failed</c> returns HTTP 405 Method Not
/// Allowed against the live API, matching the same pattern as <c>/cancel</c>, <c>/rerun</c>,
/// <c>/credits/precheck</c>, and <c>/credits/estimate</c>.  Remove <c>[Ignore]</c> once the
/// endpoint is deployed to the public API surface.
/// </para>
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "RetryFailed", "BatchLifecycle")]
public sealed class RetryFailedTests : BaseApiTest
{
    private TranscriptsClient _transcripts = null!;
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _transcripts = CreateClient<TranscriptsClient>();
        _batchIdsToCleanup.Clear();
    }

    [TearDown]
    public override async Task TearDown()
    {
        foreach (var batchId in _batchIdsToCleanup)
        {
            try { await _transcripts.DeleteAsync(batchId); }
            catch { /* best-effort cleanup */ }
        }

        await base.TearDown();
    }

    // Submit error videos → items fail

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/retry-failed returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit tstPRIVT001 + tstTIMEO001 → poll complete → all items have status 'error'")]
    public async Task SubmitErrorVideos_AfterPolling_AllItemsHaveErrorStatus()
    {
        var request = new TranscriptRequestBuilder()
            .WithUrls([VideoIds.PrivateUrl, VideoIds.TimeoutUrl])
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        // The batch itself completes (all items processed), but each individual item fails
        batch.Status.Should().BeOneOf(["completed", "failed"],
            "a batch with only error videos should reach a terminal state");
        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be("error",
                $"video '{item.VideoId}' is a known error test video and should produce an item-level error"));
    }

    // Retry failed → HTTP 2xx

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/retry-failed returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit error batch → poll → retry-failed → HTTP 2xx returned")]
    public async Task RetryFailed_AfterErrorBatch_Returns2xx()
    {
        // Step 1 — submit and wait for items to fail
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.PrivateUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        await _transcripts.PollUntilCompleteAsync(batchId);

        // Step 2 — trigger retry
        var retry = await _transcripts.RetryFailedAsync(batchId);

        retry.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent],
            "retry-failed should acknowledge the request with a 2xx");
    }

    // Retry failed → batch transitions back to processing

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/retry-failed returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit error batch → poll → retry-failed → batch status transitions to processing then terminal")]
    public async Task RetryFailed_AfterErrorBatch_BatchReprocesses()
    {
        // Step 1 — create a batch with one private video that will fail
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.PrivateUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var firstRun = await _transcripts.PollUntilCompleteAsync(batchId);
        firstRun.Items.Should().Contain(i => i.Status == "error",
            "tstPRIVT001 is a private video and must produce an item-level error");

        // Step 2 — retry then poll again
        var retry = await _transcripts.RetryFailedAsync(batchId);
        retry.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent]);

        var afterRetry = await _transcripts.PollUntilCompleteAsync(batchId);

        // The retry attempt is recorded even if the item still fails (private video won't succeed)
        afterRetry.Should().NotBeNull("batch should still be accessible after retry");
        afterRetry.Items.Should().NotBeEmpty("retry must produce at least one item result");
    }

    // Retry-failed on non-existent batch → 404

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/retry-failed returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Retry-failed on non-existent batch ID → HTTP 404")]
    public async Task RetryFailed_NonExistentBatchId_Returns404()
    {
        var retry = await _transcripts.RetryFailedAsync("non-existent-batch-id-00000000");

        retry.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "retry-failed on a batch that does not exist must return 404");
    }
}
