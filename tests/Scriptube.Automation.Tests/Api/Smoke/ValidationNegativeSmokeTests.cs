using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests verifying that the API correctly validates request payloads and returns
/// meaningful error responses for malformed or unsupported inputs.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("Transcripts")]
[AllureTag("Smoke", "Validation", "Negative")]
public sealed class ValidationNegativeSmokeTests : BaseApiTest
{
    private TranscriptsClient _transcripts = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _transcripts = new TranscriptsClient(Settings);
    }

    [TearDown]
    public override async Task TearDown()
    {
        _transcripts.Dispose();
        await base.TearDown();
    }

    [Test]
    [AllureStep("POST /api/v1/transcripts with empty URL list returns 4xx with error body")]
    public async Task SubmitTranscript_WithEmptyUrlList_Returns4xxWithError()
    {
        // Construct directly — the builder intentionally guards against empty URL lists,
        // so we bypass it to test the server-side validation.
        var request = new TranscriptRequest { Urls = [] };

        var response = await _transcripts.SubmitAsync(request);

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            because: "submitting an empty URL list must be rejected with a 4xx status code");
        response.Content.Should().NotBeNullOrWhiteSpace(
            because: "the error response body must contain a descriptive error message");
    }

    [Test]
    [AllureStep("POST /api/v1/transcripts with non-YouTube URL returns 4xx with error body")]
    public async Task SubmitTranscript_WithNonYouTubeUrl_Returns4xxWithError()
    {
        var request = new TranscriptRequest { Urls = ["https://vimeo.com/123456789"] };

        var response = await _transcripts.SubmitAsync(request);

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            because: "non-YouTube URLs must be rejected with a 4xx status code");
        response.Content.Should().NotBeNullOrWhiteSpace(
            because: "the error response body must contain a descriptive error message");
    }
}
