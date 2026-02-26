using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests verifying that every success-path test video ID produces a completed
/// transcript with non-empty text.  Each test covers a distinct ingestion path (manual
/// captions, auto-captions, translation, ElevenLabs AI, cache hits).
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Transcripts", "SuccessPaths")]
public sealed class SuccessVideoPathTests : BaseApiTest
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

    // Success Video Paths

    [Test]
    [AllureStep("tstENAUT001 — English auto-captions → completed, transcript non-empty")]
    public async Task EnglishAutoCaptions_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.EnglishAutoUrl);

        item.Status.Should().Be("completed",
            because: "tstENAUT001 is a known-good English auto-caption video");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "an auto-caption video must produce transcript text");
    }

    [Test]
    [AllureStep("tstKOONL001 — Korean → English translation → completed, transcript non-empty")]
    public async Task KoreanToEnglishTranslation_CompletesWithNonEmptyTranscript()
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.KoreanOnlyUrl)
            .WithTranslation()
            .Build();

        var item = await SubmitPollAndGetSingleItemAsync(request);

        item.Status.Should().Be("completed",
            because: "tstKOONL001 with translate_to_english is a known-good Korean test video");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a translated Korean video must produce English transcript text");
    }

    [Test]
    [AllureStep("tstESAUT001 — Spanish auto-captions → English translation → completed, transcript non-empty")]
    public async Task SpanishToEnglishTranslation_CompletesWithNonEmptyTranscript()
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.SpanishOnlyUrl)
            .WithTranslation()
            .Build();

        var item = await SubmitPollAndGetSingleItemAsync(request);

        item.Status.Should().Be("completed",
            because: "tstESAUT001 with translate_to_english is a known-good Spanish test video");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a translated Spanish video must produce English transcript text");
    }

    [Test]
    [AllureStep("tstMULTI001 — multi-language video, English track selected → completed, transcript non-empty")]
    public async Task MultiLanguage_EnglishTrackSelected_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.MultiLanguageUrl);

        item.Status.Should().Be("completed",
            because: "tstMULTI001 has an English caption track and should complete without translation");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "the English track of a multi-language video must produce transcript text");
    }

    [Test]
    [AllureStep("tstYTTRN001 — French with YouTube auto-translate → completed, transcript non-empty")]
    public async Task FrenchYouTubeAutoTranslate_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.FrenchAutoTranslateUrl);

        item.Status.Should().Be("completed",
            because: "tstYTTRN001 is a French video served via YouTube's auto-translate path");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "YouTube auto-translate must yield a non-empty transcript");
    }

    [Test]
    [AllureStep("tstNOCAP001 — no captions, ElevenLabs AI fallback → completed, transcript non-empty")]
    public async Task NoCaptions_ElevenLabsFallback_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.NoCaptionsUrl);

        item.Status.Should().Be("completed",
            because: "tstNOCAP001 has no captions but ElevenLabs AI fallback must succeed");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "ElevenLabs AI transcription must produce non-empty text");
    }

    [Test]
    [AllureStep("tstELABS001 — forced ElevenLabs transcription → completed, transcript non-empty")]
    public async Task ForcedElevenLabs_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.ElevenLabsForcedUrl);

        item.Status.Should().Be("completed",
            because: "tstELABS001 uses forced ElevenLabs transcription and must complete");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a forced ElevenLabs transcription must produce non-empty text");
    }

    [Test]
    [AllureStep("tstELTRN001 — ElevenLabs + German → English translation → completed, transcript non-empty")]
    public async Task ElevenLabsGermanToEnglishTranslation_CompletesWithNonEmptyTranscript()
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.ElevenLabsTranslationUrl)
            .WithTranslation()
            .Build();

        var item = await SubmitPollAndGetSingleItemAsync(request);

        item.Status.Should().Be("completed",
            because: "tstELTRN001 uses ElevenLabs with German → English translation and must complete");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "ElevenLabs translation must produce non-empty English transcript text");
    }

    [Test]
    [AllureStep("tstCACHE001 — cached YouTube transcript → completed, transcript non-empty")]
    public async Task CachedYouTubeTranscript_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.CachedYouTubeUrl);

        item.Status.Should().Be("completed",
            because: "tstCACHE001 exercises the cache-hit path and must return a completed transcript");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a cached YouTube transcript must contain the previously stored text");
    }

    [Test]
    [AllureStep("tstCACEL001 — cached ElevenLabs transcript → completed, transcript non-empty")]
    public async Task CachedElevenLabsTranscript_CompletesWithNonEmptyTranscript()
    {
        var item = await SubmitPollAndGetSingleItemAsync(VideoIds.CachedElevenLabsUrl);

        item.Status.Should().Be("completed",
            because: "tstCACEL001 exercises the ElevenLabs cache-hit path and must return a completed transcript");
        item.TranscriptText.Should().NotBeNullOrWhiteSpace(
            because: "a cached ElevenLabs transcript must contain the previously stored text");
    }

    // Helpers

    /// <summary>
    /// Submits a single URL (built with default <see cref="TranscriptRequestBuilder"/> settings),
    /// polls to completion, and returns the single <see cref="TranscriptItemResponse"/>.
    /// </summary>
    private Task<Scriptube.Automation.Api.Models.Responses.TranscriptItemResponse>
        SubmitPollAndGetSingleItemAsync(string url)
    {
        var request = new TranscriptRequestBuilder()
            .WithUrl(url)
            .Build();
        return SubmitPollAndGetSingleItemAsync(request);
    }

    /// <summary>
    /// Submits a pre-built <see cref="Scriptube.Automation.Api.Models.Requests.TranscriptRequest"/>,
    /// polls to completion, and returns the single <see cref="TranscriptItemResponse"/>.
    /// </summary>
    private async Task<Scriptube.Automation.Api.Models.Responses.TranscriptItemResponse>
        SubmitPollAndGetSingleItemAsync(Scriptube.Automation.Api.Models.Requests.TranscriptRequest request)
    {
        var submitResponse = await _transcripts.SubmitAsync(request);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted,
            because: "a valid batch submission must return HTTP 202 Accepted");
        submitResponse.Data.Should().NotBeNull();

        var batchId = submitResponse.Data!.BatchId;
        batchId.Should().NotBeNullOrWhiteSpace(because: "a submitted batch must receive a non-empty batch ID");
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);

        batch.Items.Should().HaveCount(1,
            because: "a single-URL submission must produce exactly one transcript item");

        return batch.Items[0];
    }
}
