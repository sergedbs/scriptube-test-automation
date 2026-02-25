using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Smoke;

/// <summary>
/// Smoke tests for <c>POST /tools/youtube-transcript</c> (public, no-auth SEO tool endpoint).
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("API")]
[AllureSuite("Smoke")]
[AllureFeature("SEO Tool")]
[AllureTag("Smoke", "SEO", "NoAuth")]
[Ignore("POST /tools/youtube-transcript is not present in the public OpenAPI spec — endpoint does not exist.")]
public sealed class SeoToolSmokeTests : BaseApiTest
{
    private SeoToolClient _seoTool = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _seoTool = new SeoToolClient(Settings);
    }

    [TearDown]
    public override async Task TearDown()
    {
        _seoTool.Dispose();
        await base.TearDown();
    }

    [Test]
    [AllureStep("POST /tools/youtube-transcript (no auth) returns HTTP 200 for known video")]
    public async Task GetTranscript_WithValidVideo_ReturnsHttp200()
    {
        var response = await _seoTool.GetTranscriptAsync(VideoIds.EnglishManualUrl);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            because: "the public SEO tool must return HTTP 200 for a valid, known video URL");
    }

    [Test]
    [AllureStep("POST /tools/youtube-transcript (no auth) returns non-empty transcript content")]
    public async Task GetTranscript_WithValidVideo_ReturnsTranscriptContent()
    {
        var response = await _seoTool.GetTranscriptAsync(VideoIds.EnglishManualUrl);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Should().NotBeNullOrWhiteSpace(
            because: "the response body must contain transcript data for a valid video");
    }
}
