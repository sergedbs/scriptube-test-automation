using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests verifying that every error-path test video ID produces a batch-level
/// terminal status with item status "failed" and a non-empty error message.
/// The batch itself is expected to complete (all items processed), but each individual item
/// must reflect the failure reason via the error field.
/// Note: the live API uses status "failed" at the item level, not "error".
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Transcripts", "ErrorPaths")]
public sealed class ErrorVideoPathTests : BaseApiTest
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
    public override async Task TearDown()
    {
        foreach (var batchId in _batchIdsToCleanup)
        {
            try { await _transcripts.DeleteAsync(batchId); }
            catch { /* best-effort cleanup */ }
        }

        _transcripts.Dispose();
        await base.TearDown();
    }

    // Error Video Paths

    [Test]
    [AllureStep("tstPRIVT001 — private video — item status is error, error message present")]
    public async Task PrivateVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.PrivateUrl);

        item.Status.Should().Be("failed",
            because: "tstPRIVT001 is a private video and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "a private video error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstDELET001 — deleted video — item status is error, error message present")]
    public async Task DeletedVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.DeletedUrl);

        item.Status.Should().Be("failed",
            because: "tstDELET001 is a deleted video and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "a deleted video error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstAGERS001 — age-restricted video — item status is error, error message present")]
    public async Task AgeRestrictedVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.AgeRestrictedUrl);

        item.Status.Should().Be("failed",
            because: "tstAGERS001 is an age-restricted video and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "an age-restricted video error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstLONG0001 — video too long — item status is error, error message present")]
    public async Task TooLongVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.TooLongUrl);

        item.Status.Should().Be("failed",
            because: "tstLONG0001 exceeds the maximum duration limit and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "an oversized video error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstRLIMT001 — rate-limited video — item status is error, error message present")]
    public async Task RateLimitedVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.RateLimitedUrl);

        item.Status.Should().Be("failed",
            because: "tstRLIMT001 triggers a rate-limit condition and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "a rate-limit error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstTIMEO001 — connection timeout — item status is error, error message present")]
    public async Task TimeoutVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.TimeoutUrl);

        item.Status.Should().Be("failed",
            because: "tstTIMEO001 simulates a connection timeout and must produce an item-level failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "a timeout error must include a descriptive message");
    }

    [Test]
    [AllureStep("tstINVLD001 — malformed video data — item status is error, error message present")]
    public async Task InvalidVideo_ItemStatusIsError_ErrorMessagePresent()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.InvalidUrl);

        item.Status.Should().Be("failed",
            because: "tstINVLD001 contains malformed data that triggers a processing failure");
        item.Error.Should().NotBeNullOrWhiteSpace(
            because: "a malformed video error must include a descriptive message");
    }

    // Helpers

    /// <summary>
    /// Submits a single URL, polls to a terminal batch status, asserts the batch reached
    /// a terminal state, and returns the single item response.
    /// </summary>
    private async Task<Scriptube.Automation.Api.Models.Responses.TranscriptItemResponse>
        SubmitPollAndGetSingleItemAsync(string url)
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(url)
            .Build();

        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "even error-path videos must be accepted for processing with HTTP 202");
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        batchId.Should().NotBeNullOrWhiteSpace(because: "a submitted batch must receive a non-empty batch ID");
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        batch.Status.Should().BeOneOf(
            ["completed", "failed"],
            because: "a batch containing only an error video must still reach a terminal state");
        batch.Items.Should().HaveCount(1,
            because: "a single-URL submission must produce exactly one transcript item");

        return batch.Items[0];
    }
}
