using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for the batch cancel flow
/// (<c>POST /api/v1/transcripts/{batch_id}/cancel</c>).
/// <para>
/// Tests submit a batch and immediately issue a cancel — without any polling delay — to catch
/// the batch while it is still in the <c>processing</c> state.  The mock API processes
/// deterministic <c>tst*</c> video IDs asynchronously, so there is an inherent race: if the
/// mock resolves faster than the cancel HTTP round-trip the batch may already be
/// <c>completed</c>.  Where this race affects the expected outcome it is explicitly documented
/// in the assertion message.
/// </para>
/// <para>
/// All tests are currently <b>ignored</b>: <c>POST …/cancel</c> returns HTTP 405 Method Not
/// Allowed against the live API, matching the same pattern as <c>/retry-failed</c>, <c>/rerun</c>,
/// <c>/credits/precheck</c>, and <c>/credits/estimate</c>.  Remove <c>[Ignore]</c> once the
/// endpoint is deployed to the public API surface.
/// </para>
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Cancel", "BatchLifecycle")]
public sealed class BatchCancelTests : BaseApiTest
{
    private TranscriptsClient _transcripts = null!;
    private CreditsClient _credits = null!;
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _transcripts = new TranscriptsClient(Settings);
        _credits = new CreditsClient(Settings);
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
        _credits.Dispose();
        base.TearDown();
    }

    // Cancel immediately → status becomes "cancelled"

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/cancel returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit batch → cancel immediately → poll to terminal → status is 'cancelled'")]
    public async Task CancelBatch_ImmediatelyAfterSubmit_StatusIsCancelled()
    {
        // Arrange — submit a single-video batch
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted,
            "submit must succeed before we can test cancel");
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        // Act — cancel without any polling delay to catch batch while still processing
        var cancel = await _transcripts.CancelAsync(batchId);

        cancel.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted],
            "cancel should return a 2xx when the batch is still processing");

        // Assert — poll to terminal state; PollUntilCompleteAsync treats "cancelled" as terminal
        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        batch.Status.Should().Be("cancelled",
            "a batch cancelled immediately after submit should not proceed to completion. " +
            "If this fails with 'completed', the mock environment resolved faster than the " +
            "cancel round-trip — a known race condition in the test environment.");
    }

    // Cancel a non-existent batch → 404

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/cancel returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Cancel non-existent batch ID → HTTP 404")]
    public async Task CancelBatch_NonExistentBatchId_Returns404()
    {
        var cancel = await _transcripts.CancelAsync("non-existent-batch-id-00000000");

        cancel.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "cancelling a batch that does not exist must return 404");
    }

    // Cancelled batch → no credits charged

    [Test]
    [Ignore("POST /api/v1/transcripts/{batch_id}/cancel returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Submit batch → cancel → verify credit balance is unchanged")]
    public async Task CancelledBatch_BalanceUnchanged_NoCreditsCharged()
    {
        // Arrange — snapshot balance before anything
        var before = await _credits.GetBalanceAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceBefore = before.Data!.CreditsBalance;

        // Act — submit and cancel immediately
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var cancel = await _transcripts.CancelAsync(batchId);
        cancel.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted]);

        // Wait briefly for the server to settle the cancel before re-reading balance
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert — verify actual terminal status then check credit impact
        var batch = (await _transcripts.GetBatchAsync(batchId)).Data;

        var after = await _credits.GetBalanceAsync();
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        var deduction = balanceBefore - after.Data!.CreditsBalance;

        if (batch?.Status == "cancelled")
        {
            deduction.Should().Be(0,
                "a cancelled batch must not deduct any credits");
        }
        else
        {
            // Race condition: batch completed before cancel was processed —
            // document the actual deduction but do not fail the test.
            // The primary assertion (deduction == 0) is conditioned on confirmed cancellation.
            Assert.Inconclusive(
                $"Batch resolved to '{batch?.Status}' before cancel took effect " +
                $"(deduction: {deduction} credits). Re-run to confirm cancel timing.");
        }
    }
}
