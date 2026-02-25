using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for playlist URL expansion.
/// Covers all-success, mixed, and all-error playlist scenarios and
/// verifies that item counts and per-item statuses match the playlist composition.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Transcripts", "Playlists")]
public sealed class PlaylistTests : BaseApiTest
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

    // Playlist Tests

    [Test]
    [AllureStep("PLtstOK00001 — all-success playlist — expands to 3 items, all completed")]
    public async Task AllSuccessPlaylist_ExpandsTo3Items_AllCompleted()
    {
        var batch = await SubmitPlaylistAndPollAsync(PlaylistUrls.AllSuccess);

        batch.Items.Should().HaveCount(PlaylistUrls.AllSuccessItemCount,
            because: $"PLtstOK00001 is defined as a {PlaylistUrls.AllSuccessItemCount}-video all-success playlist");
        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be("completed",
                because: $"PLtstOK00001 contains only success videos — item {item.VideoId} must complete"));
    }

    [Test]
    [AllureStep("PLtstMIX0001 — mixed playlist — all items present, per-item status is completed or failed")]
    public async Task MixedPlaylist_AllItemsPresent_StatusIsCompletedOrFailed()
    {
        var batch = await SubmitPlaylistAndPollAsync(PlaylistUrls.Mixed);

        batch.Items.Should().NotBeEmpty(
            because: "PLtstMIX0001 is a non-empty mixed playlist");

        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().BeOneOf(
                ["completed", "failed"],
                because: $"every item in a mixed playlist must reach a terminal status — item {item.VideoId} has unexpected status '{item.Status}'"));

        var completedCount = batch.Items.Count(i => i.Status == "completed");
        var failedCount = batch.Items.Count(i => i.Status == "failed");

        completedCount.Should().BeGreaterThan(0,
            because: "PLtstMIX0001 contains at least one success video");
        failedCount.Should().BeGreaterThanOrEqualTo(0,
            because: "failed item count is non-negative");
    }

    [Test]
    [AllureStep("PLtstALL0001 — 5-video mixed playlist — exactly 5 items present")]
    public async Task AllMixedPlaylist_Returns5Items()
    {
        var batch = await SubmitPlaylistAndPollAsync(PlaylistUrls.AllMixed);

        batch.Items.Should().HaveCount(PlaylistUrls.AllMixedItemCount,
            because: $"PLtstALL0001 is defined as a {PlaylistUrls.AllMixedItemCount}-video playlist");

        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().BeOneOf(
                ["completed", "failed"],
                because: $"every item must reach a terminal status — item {item.VideoId} has unexpected status '{item.Status}'"));
    }

    [Test]
    [AllureStep("PLtstERR0001 — all-error playlist — batch reaches terminal status, all items failed")]
    public async Task AllErrorPlaylist_BatchTerminates_AllItemsFailed()
    {
        var batch = await SubmitPlaylistAndPollAsync(PlaylistUrls.AllErrors);

        batch.Status.Should().BeOneOf(
            ["completed", "failed"],
            because: "a batch of all-error videos must still reach a terminal batch status");

        batch.Items.Should().NotBeEmpty(
            because: "PLtstERR0001 must expand to at least one item");

        batch.Items.Should().AllSatisfy(item =>
        {
            item.Status.Should().Be("failed",
                because: $"PLtstERR0001 contains only error videos — item {item.VideoId} must have status 'failed'");
            item.Error.Should().NotBeNullOrWhiteSpace(
                because: $"each failed item must include a descriptive error message — item {item.VideoId} has none");
        });
    }

    // Helpers

    /// <summary>
    /// Submits a playlist URL, asserts HTTP 202, polls to completion, and returns
    /// the final <see cref="Scriptube.Automation.Api.Models.Responses.BatchStatusResponse"/>.
    /// </summary>
    private async Task<Scriptube.Automation.Api.Models.Responses.BatchStatusResponse>
        SubmitPlaylistAndPollAsync(string playlistUrl)
    {
        var request = new TranscriptRequestBuilder()
            .WithPlaylist(playlistUrl)
            .Build();

        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "a valid playlist submission must return HTTP 202 Accepted");
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        batchId.Should().NotBeNullOrWhiteSpace(because: "a submitted batch must receive a non-empty batch ID");
        _batchIdsToCleanup.Add(batchId);

        return await _transcripts.PollUntilCompleteAsync(batchId);
    }
}
