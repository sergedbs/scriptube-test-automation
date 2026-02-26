using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for Precheck → Submit cost correlation
/// (<c>POST /api/v1/credits/precheck</c> and <c>POST /api/v1/credits/estimate</c>).
/// <para>
/// All tests in this class are currently <b>ignored</b>: both endpoints return HTTP 405 Method Not
/// Allowed against the live API, indicating they are not yet part of the public surface.  The test
/// bodies are complete and intentional — re-enable by removing the <c>[Ignore]</c> attributes once
/// the endpoints are deployed.
/// </para>
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Credits")]
[AllureTag("Regression", "API", "Credits", "Precheck", "Estimate")]
public sealed class PrecheckEstimateTests : BaseApiTest
{
    private CreditsClient _credits = null!;
    private TranscriptsClient _transcripts = null!;
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _credits = CreateClient<CreditsClient>();
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

    // Precheck

    [Test]
    [Ignore("POST /api/v1/credits/precheck returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Precheck tstENMAN001 URL → HTTP 200, estimated_cost > 0")]
    public async Task PrecheckWithValidUrl_ReturnsPositiveEstimatedCost()
    {
        var request = new PrecheckRequest { Urls = [VideoIds.EnglishManualUrl] };

        var response = await _credits.PrecheckAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a valid YouTube URL should pass precheck and return a cost estimate");
        response.Data.Should().NotBeNull();
        response.Data!.EstimatedCost.Should().BeGreaterThan(0,
            "processing at least one URL always costs credits");
    }

    [Test]
    [Ignore("POST /api/v1/credits/precheck returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Precheck multiple URLs → item-level cost breakdown matches sum of estimated_cost")]
    public async Task PrecheckMultipleUrls_ItemCostsSumMatchesTotalEstimate()
    {
        var request = new PrecheckRequest
        {
            Urls =
            [
                VideoIds.EnglishManualUrl,
                VideoIds.EnglishAutoUrl,
                VideoIds.KoreanOnlyUrl
            ]
        };

        var response = await _credits.PrecheckAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();

        var data = response.Data!;
        data.Items.Should().HaveCount(3, "one item per submitted URL");

        var itemTotal = data.Items.Sum(i => i.EstimatedCost);
        itemTotal.Should().Be(data.EstimatedCost,
            "sum of per-item costs must equal the top-level estimated_cost");
    }

    [Test]
    [Ignore("POST /api/v1/credits/precheck returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Precheck empty URL list → HTTP 422 validation error")]
    public async Task PrecheckEmptyUrlList_Returns422()
    {
        var request = new PrecheckRequest { Urls = [] };

        var response = await _credits.PrecheckAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "submitting an empty URL list is a validation error");
    }

    // Estimate

    [Test]
    [Ignore("POST /api/v1/credits/estimate returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Estimate tstENMAN001 video ID → HTTP 200, cost > 0")]
    public async Task EstimateVideoId_ReturnsPositiveCost()
    {
        var request = new EstimateRequest { VideoIds = [VideoIds.EnglishManual] };

        var response = await _credits.EstimateAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a valid test video ID should return a cost estimate");
        response.Data.Should().NotBeNull();
        response.Data!.EstimatedCost.Should().BeGreaterThan(0);
    }

    // Correlation: precheck estimate == actual deduction

    [Test]
    [Ignore("POST /api/v1/credits/precheck returns HTTP 405 — endpoint absent from public API.")]
    [AllureStep("Precheck estimated cost equals actual credits deducted after batch completes")]
    public async Task PrecheckEstimatedCost_MatchesActualDeduction()
    {
        // Step 1 — precheck to retrieve estimated cost
        var precheck = await _credits.PrecheckAsync(
            new PrecheckRequest { Urls = [VideoIds.EnglishManualUrl] });
        precheck.StatusCode.Should().Be(HttpStatusCode.OK);
        var estimatedCost = precheck.Data!.EstimatedCost;

        // Step 2 — snapshot balance
        var before = await _credits.GetBalanceAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceBefore = before.Data!.CreditsBalance;

        // Step 3 — submit and poll
        var submit = await _transcripts.SubmitAsync(
            new TranscriptRequestBuilder()
                .WithUrl(VideoIds.EnglishManualUrl)
                .Build());
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);
        batch.Status.Should().Be("completed");

        // Step 4 — actual deduction must equal precheck estimate
        var after = await _credits.GetBalanceAsync();
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualDeduction = balanceBefore - after.Data!.CreditsBalance;

        actualDeduction.Should().Be(estimatedCost,
            "the precheck estimate must exactly match the credits charged at completion");
    }
}
