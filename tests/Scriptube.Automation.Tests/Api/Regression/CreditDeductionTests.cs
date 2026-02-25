using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests verifying credit deduction after batch processing.
/// Actual costs per the live API: 4 credits per transcript,
/// plus 12 credits per 1,000 characters for translation.
/// <para>
/// <see cref="PrecheckEstimate_MatchesActualDeduction_ForEnglishManualVideo"/> is currently
/// <b>ignored</b>: <c>POST /api/v1/credits/precheck</c> returns HTTP 405 Method Not Allowed
/// against the live API, matching the same pattern as <c>/credits/estimate</c>, <c>/cancel</c>,
/// <c>/retry-failed</c>, and <c>/rerun</c>.  Remove <c>[Ignore]</c> once the endpoint is
/// deployed to the public API surface.
/// </para>
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Credits")]
[AllureTag("Regression", "API", "Credits", "Deduction")]
public sealed class CreditDeductionTests : BaseApiTest
{
    private CreditsClient _credits = null!;
    private TranscriptsClient _transcripts = null!;
    private readonly List<string> _batchIdsToCleanup = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _credits = new CreditsClient(Settings);
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

        _credits.Dispose();
        _transcripts.Dispose();
        await base.TearDown();
    }

    [Test]
    [AllureStep("Balance before submit is a valid non-negative integer")]
    public async Task GetBalance_BeforeSubmit_IsNonNegative()
    {
        var response = await _credits.GetBalanceAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.CreditsBalance.Should().BeGreaterThanOrEqualTo(0,
            "credit balance must never be negative");
    }

    [Test]
    [AllureStep("Submit tstENMAN001 → balance decreases by exactly 4 credits")]
    public async Task SubmitEnglishManual_BalanceDecreasedBy4Credits()
    {
        // Arrange — snapshot balance before
        var before = await _credits.GetBalanceAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceBefore = before.Data!.CreditsBalance;

        // Act — submit single English manual video (4 credits per live API)
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        await _transcripts.PollUntilCompleteAsync(batchId);

        // Assert — balance after must be exactly 4 less
        var after = await _credits.GetBalanceAsync();
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceAfter = after.Data!.CreditsBalance;

        (balanceBefore - balanceAfter).Should().Be(4,
            "tstENMAN001 is a standard English caption video and costs exactly 4 credits");
    }

    [Test]
    [AllureStep("Submit tstKOONL001 with translate_to_english → batch completes and deduction is at least 4 credits")]
    public async Task SubmitKoreanWithTranslation_BatchCompletesAndDeductionAtLeast4Credits()
    {
        // Arrange — snapshot balance before
        var before = await _credits.GetBalanceAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceBefore = before.Data!.CreditsBalance;

        // Act — submit Korean video with translate_to_english flag set
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.KoreanOnlyUrl)
            .WithTranslation()
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        var batch = await _transcripts.PollUntilCompleteAsync(batchId);
        batch.Status.Should().Be("completed",
            "tstKOONL001 is a known-good Korean test video");

        // Assert — deduction is at least the base transcript cost.
        // Note: tst* mock video IDs charge a flat credit cost regardless of the translate_to_english
        // flag — real-world translation billing (12 credits/1K chars) applies only to live YouTube videos.
        var after = await _credits.GetBalanceAsync();
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        var deduction = balanceBefore - after.Data!.CreditsBalance;

        deduction.Should().BeGreaterThanOrEqualTo(4,
            "processing any transcript costs at least 4 credits");
    }

    [Test]
    [Ignore("POST /api/v1/credits/precheck returns HTTP 405 — endpoint is absent from the public OpenAPI spec.")]
    [AllureStep("Precheck estimated cost matches actual balance deduction for tstENMAN001")]
    public async Task PrecheckEstimate_MatchesActualDeduction_ForEnglishManualVideo()
    {
        // Arrange — precheck to get the estimate
        var precheckRequest = new PrecheckRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var precheckResponse = await _credits.PrecheckAsync(precheckRequest);
        precheckResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        precheckResponse.Data.Should().NotBeNull();

        var estimatedCost = precheckResponse.Data!.EstimatedCost;
        estimatedCost.Should().BeGreaterThan(0,
            "precheck must return a positive estimated cost for a valid video URL");

        // Arrange — snapshot balance after precheck (precheck itself is free)
        var before = await _credits.GetBalanceAsync();
        var balanceBefore = before.Data!.CreditsBalance;

        // Act — submit the same URL and poll to completion
        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submit = await _transcripts.SubmitAsync(request);
        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var batchId = submit.Data!.BatchId;
        _batchIdsToCleanup.Add(batchId);

        await _transcripts.PollUntilCompleteAsync(batchId);

        // Assert — actual deduction must equal the precheck estimate
        var after = await _credits.GetBalanceAsync();
        var actualDeduction = balanceBefore - after.Data!.CreditsBalance;

        actualDeduction.Should().Be(estimatedCost,
            $"the precheck estimated {estimatedCost} credits and the actual deduction after processing must match");
    }
}
