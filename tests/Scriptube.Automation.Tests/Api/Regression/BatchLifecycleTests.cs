using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for the batch lifecycle operations:
/// rerun (<c>POST /api/v1/transcripts/{batch_id}/rerun</c>) and
/// delete (<c>DELETE /api/v1/transcripts/{batch_id}</c>).
/// <para>
/// All tests are currently <b>ignored</b>: both <c>POST …/rerun</c> and
/// <c>DELETE /api/v1/transcripts/{batch_id}</c> return HTTP 405 Method Not Allowed against
/// the live API, matching the same pattern as <c>/cancel</c>, <c>/retry-failed</c>,
/// <c>/credits/precheck</c>, and <c>/credits/estimate</c>.  Remove <c>[Ignore]</c> once the
/// endpoints are deployed to the public API surface.
/// </para>
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "BatchLifecycle", "Delete", "Rerun")]
public sealed class BatchLifecycleTests : BaseApiTest
{
    private TranscriptsClient _transcripts = null!;
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _transcripts = new TranscriptsClient(Settings);
        _batchIdsToCleanup.Clear();
    }

    [TearDown]
    public override void TearDown()
    {
        foreach (var batchId in _batchIdsToCleanup)
        {
            try { _transcripts.DeleteAsync(batchId).GetAwaiter().GetResult(); }
            catch { /* best-effort cleanup */ }
        }

        _transcripts.Dispose();
        base.TearDown();
    }

    // Rerun

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/rerun returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit batch → poll → rerun → batch re-enters processing")]
    public async Task RerunBatch_AfterCompletion_BatchRestartsProcessing()
    {
        // Step 1 — submit and wait for completion
        var submit = await _transcripts.SubmitAsync(
            new TranscriptRequestBuilder()
                .WithUrl(VideoIds.EnglishManualUrl)
                .Build());

        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var firstRun = await _transcripts.PollUntilCompleteAsync(batchId);
        firstRun.Status.Should().Be("completed");

        // Step 2 — rerun
        var rerun = await _transcripts.RerunAsync(batchId);

        rerun.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent],
            "rerun should acknowledge the request with a 2xx");

        // Step 3 — poll the rerun to terminal
        var afterRerun = await _transcripts.PollUntilCompleteAsync(batchId);

        afterRerun.Status.Should().BeOneOf(["completed", "failed"],
            "batch must reach a terminal state after rerun");
        afterRerun.Items.Should().NotBeEmpty(
            "rerun must produce at least one item result");
    }

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/rerun returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Rerun non-existent batch ID → HTTP 404")]
    public async Task RerunBatch_NonExistentBatchId_Returns404()
    {
        var rerun = await _transcripts.RerunAsync("non-existent-batch-id-00000000");

        rerun.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "rerun on a batch that does not exist must return 404");
    }

    // Delete

    [Test]
    [Ignore("DELETE /api/v1/transcripts/{batch_id} returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit batch → poll complete → DELETE batch → 200/204")]
    public async Task DeleteBatch_ExistingCompletedBatch_Returns2xx()
    {
        // Arrange — submit and complete a batch
        var submit = await _transcripts.SubmitAsync(
            new TranscriptRequestBuilder()
                .WithUrl(VideoIds.EnglishManualUrl)
                .Build());

        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;

        await _transcripts.PollUntilCompleteAsync(batchId);

        // Act — delete
        var delete = await _transcripts.DeleteAsync(batchId);

        delete.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.NoContent],
            "DELETE on an existing batch must return 200 or 204");
    }

    [Test]
    [Ignore("DELETE /api/v1/transcripts/{batch_id} returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit batch → poll complete → DELETE batch → GET batch → 404")]
    public async Task DeleteBatch_ExistingBatch_SubsequentGetReturns404()
    {
        // Arrange — create and complete a batch
        var submit = await _transcripts.SubmitAsync(
            new TranscriptRequestBuilder()
                .WithUrl(VideoIds.EnglishManualUrl)
                .Build());

        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        // NOTE: NOT added to _batchIdsToCleanup — this test deletes the batch itself

        await _transcripts.PollUntilCompleteAsync(batchId);

        // Act — delete the batch
        await _transcripts.DeleteAsync(batchId);

        // Assert — subsequent GET must return 404
        var get = await _transcripts.GetBatchAsync(batchId);

        get.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "fetching a deleted batch by its ID must return 404");
    }

    [Test]
    [Ignore("DELETE /api/v1/transcripts/{batch_id} returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("DELETE non-existent batch ID → HTTP 404")]
    public async Task DeleteBatch_NonExistentBatchId_Returns404()
    {
        var delete = await _transcripts.DeleteAsync("non-existent-batch-id-00000000");

        delete.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleting a batch that does not exist must return 404");
    }
}
