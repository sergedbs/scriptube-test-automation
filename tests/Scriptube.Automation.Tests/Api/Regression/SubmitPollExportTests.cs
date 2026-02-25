using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for the core Submit → Poll → Export E2E flow.
/// Covers single video, multi-video batch, playlist expansion, and all export formats.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Transcripts", "E2E")]
public sealed class SubmitPollExportTests : BaseApiTest
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

    // Submit → Poll → Export

    [Test]
    [AllureStep("Submit single English manual video → poll to completion → transcript text non-empty")]
    public async Task SubmitSingleVideo_EnglishManual_TranscriptTextNonEmpty()
    {
        // Arrange
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        // Act — submit
        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "a valid batch submission must return HTTP 202 Accepted");
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        batchId.Should().NotBeNullOrWhiteSpace(because: "a submitted batch must receive a non-empty batch ID");
        _batchIdsToCleanup.Add(batchId);

        // Act — poll
        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        // Assert — batch level
        batch.Status.Should().BeOneOf(
            ["completed", "failed"],
            "polling must end in a terminal status");
        batch.Items.Should().HaveCount(1,
            because: "a single-URL submission must produce exactly one transcript item");

        // Assert — item level
        var item = batch.Items[0];
        item.Status.Should().Be("completed",
            because: "tstENMAN001 is a known-good English manual-caption video");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a completed English manual-caption transcript must contain text");
    }

    [Test]
    [AllureStep("Submit batch of 3 success videos → all items complete")]
    public async Task SubmitMultipleVideos_ThreeSuccessVideos_AllItemsComplete()
    {
        // Arrange — three distinct success paths
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .WithUrl(VideoIds.EnglishAutoUrl)
            .WithUrl(VideoIds.MultiLanguageUrl)
            .Build();

        // Act — submit
        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        submitResponse.Data.Should().NotBeNull();
        submitResponse.Data!.UrlCount.Should().Be(3,
            because: "three URLs were submitted so url_count must equal 3");

        var batchId = submitResponse.Data.BatchId;
        _batchIdsToCleanup.Add(batchId);

        // Act — poll
        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        // Assert
        batch.Items.Should().HaveCount(3,
            because: "a 3-URL submission must produce exactly 3 transcript items");
        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be("completed",
                because: $"all three success videos must complete — item {item.VideoId} failed with: {item.Error}"));
    }

    [Test]
    [AllureStep("Submit playlist URL → batch expands to expected item count")]
    public async Task SubmitPlaylistUrl_AllSuccessPlaylist_ExpandsToItems()
    {
        // Arrange
        var request = new TranscriptRequestBuilder()
            .WithPlaylist(PlaylistUrls.AllSuccess)
            .Build();

        // Act — submit
        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        // Act — poll
        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        // Assert — playlist PLtstOK00001 contains 3 success videos per TASK spec
        batch.Items.Should().HaveCountGreaterThanOrEqualTo(1,
            because: "a playlist submission must expand to at least one video");
        batch.Items.Should().NotBeEmpty(
            because: "PLtstOK00001 is a 3-video all-success test playlist");
        batch.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be("completed",
                because: $"PLtstOK00001 contains only success videos — item {item.VideoId} failed with: {item.Error}"));
    }

    [Test]
    [AllureStep("Export completed batch in JSON format → valid JSON array with expected fields")]
    public async Task Export_JsonFormat_ValidJsonArrayShape()
    {
        // Arrange — submit and poll a single-video batch
        var batchId = await SubmitAndPollSingleVideoAsync(VideoIds.EnglishManualUrl);

        // Act — export
        var exportResponse = await _transcripts.ExportAsync(batchId, ExportFormats.Json);

        // Assert
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a completed batch must be exportable as JSON");
        exportResponse.Content.Should().NotBeNullOrWhiteSpace(
            because: "JSON export must return a non-empty body");

        // Validate it is parseable JSON and contains expected top-level keys
        using var doc = JsonDocument.Parse(exportResponse.Content!);
        var root = doc.RootElement;

        root.ValueKind.Should().BeOneOf(
            [JsonValueKind.Array, JsonValueKind.Object],
            "JSON export must be a valid JSON structure at the root");

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var first = root[0];
            first.TryGetProperty("video_id", out _).Should().BeTrue(
                because: "each JSON export item must contain a 'video_id' field");
        }
    }

    [Test]
    [AllureStep("Export completed batch in TXT format → non-empty plain text response")]
    public async Task Export_TxtFormat_NonEmptyPlainTextContent()
    {
        // Arrange
        var batchId = await SubmitAndPollSingleVideoAsync(VideoIds.EnglishManualUrl);

        // Act
        var exportResponse = await _transcripts.ExportAsync(batchId, ExportFormats.Txt);

        // Assert
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a completed batch must be exportable as TXT");
        exportResponse.Content.Should().NotBeNullOrWhiteSpace(
            because: "TXT export must contain transcript text");

        // Plain text should not begin with a JSON character
        var content = exportResponse.Content!.TrimStart();
        content.Should().NotStartWith("{", because: "TXT export must not return a JSON object");
        content.Should().NotStartWith("[", because: "TXT export must not return a JSON array");
    }

    [Test]
    [AllureStep("Export completed batch in SRT format → content contains SRT timestamp markers")]
    public async Task Export_SrtFormat_ContainsSrtTimestamps()
    {
        // Arrange
        var batchId = await SubmitAndPollSingleVideoAsync(VideoIds.EnglishManualUrl);

        // Act
        var exportResponse = await _transcripts.ExportAsync(batchId, ExportFormats.Srt);

        // Assert
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a completed batch must be exportable as SRT");
        exportResponse.Content.Should().NotBeNullOrWhiteSpace(
            because: "SRT export must contain subtitle data");

        // SRT format: "HH:MM:SS,mmm --> HH:MM:SS,mmm"
        var srtTimestampPattern = new Regex(
            @"\d{2}:\d{2}:\d{2},\d{3}\s+-->\s+\d{2}:\d{2}:\d{2},\d{3}",
            RegexOptions.Multiline);

        srtTimestampPattern.IsMatch(exportResponse.Content!).Should().BeTrue(
            because: "a valid SRT export must contain at least one 'HH:MM:SS,mmm --> HH:MM:SS,mmm' timestamp line");
    }

    // Shared helper

    /// <summary>
    /// Submits a single-URL batch, polls until completion, registers the batch ID for cleanup,
    /// and returns the batch ID for use in export tests.
    /// </summary>
    private async Task<string> SubmitAndPollSingleVideoAsync(string videoUrl)
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(videoUrl)
            .Build();

        var submitResponse = await _transcripts.SubmitAsync(request);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);
        batch.Status.Should().BeOneOf("completed", "failed");
        batch.Items.Should().ContainSingle(
            i => i.Status == "completed",
            "the helper expects at least one completed item before export");

        return batchId;
    }
}
