using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Models.Builders;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Reporting;
using Scriptube.Automation.Ui.Navigation;
using Scriptube.Automation.Ui.Pages;
using Scriptube.Automation.Ui.Tests;

namespace Scriptube.Automation.Tests.Ui.Regression;

/// <summary>
/// Regression tests for the batch submit → detail → export UI flow.
/// Requires an authenticated session via <see cref="AuthenticatedUiTest"/>.
///
/// Tests that verify a <em>completed</em> batch reuse a single batch submitted via the API in
/// <c>[OneTimeSetUp]</c>, cutting the wall-clock time from three independent polls to one.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("UI")]
[AllureSuite("Regression")]
[AllureFeature("Batch")]
[AllureTag("Regression", "UI", "Batch")]
public sealed class BatchTests : AuthenticatedUiTest
{
    // Shared between all tests that need a completed batch.
    // Submitted once via the API in [OneTimeSetUp] so individual tests pay no poll cost.
    private string? _completedBatchId;
    private TranscriptsClient _transcripts = null!;

    private DashboardPage _dashboard = null!;

    // -------------------------------------------------------------------------
    // Fixture-level setup / teardown (runs once per test class, not per test)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Submits a single transcript batch via the API and polls until it completes.
    /// <see cref="ConfigurationProvider.Get"/> is safe to call here because the provider
    /// caches settings as a singleton — no NUnit <c>[SetUp]</c> required.
    /// </summary>
    [OneTimeSetUp]
    public async Task SubmitAndAwaitCompletedBatchAsync()
    {
        var settings = ConfigurationProvider.Get();
        _transcripts = new TranscriptsClient(settings);
        AllureRestLogger.Attach(_transcripts);

        var request = new TranscriptRequestBuilder()
            .WithUrl(VideoIds.EnglishManualUrl)
            .Build();

        var submitResponse = await _transcripts.SubmitAsync(request);
        _completedBatchId = submitResponse.Data!.BatchId;

        await _transcripts.PollUntilCompleteAsync(_completedBatchId);
    }

    /// <summary>Deletes the shared batch and disposes the API client.</summary>
    [OneTimeTearDown]
    public async Task DeleteSharedBatchAsync()
    {
        if (_completedBatchId is not null)
        {
            // Best-effort: never let cleanup fail the suite.
            try { await _transcripts.DeleteAsync(_completedBatchId); } catch { /* ignored */ }
        }
        AllureRestLogger.Detach(_transcripts);
        _transcripts.Dispose();
    }

    // -------------------------------------------------------------------------
    // Per-test setup
    // -------------------------------------------------------------------------

    [SetUp]
    public async Task SetUpDashboardAsync()
    {
        _dashboard = new DashboardPage(Page);
        await _dashboard.NavigateToAsync(PageUrl(UiRoutes.Dashboard));
        await _dashboard.WaitForLoadAsync();
    }

    // -------------------------------------------------------------------------
    // Tests that exercise UI batch submission (no completed-batch dependency)
    // -------------------------------------------------------------------------

    [Test]
    [AllureStep("Submit a single URL — batch row appears in the dashboard list")]
    public async Task SubmitBatch_SingleUrl_BatchRowAppearsInList()
    {
        await _dashboard.SubmitBatchAsync(VideoIds.EnglishManualUrl);

        // Navigate back to the dashboard to verify the newly created batch is listed.
        await _dashboard.NavigateToAsync(PageUrl(UiRoutes.Dashboard));

        var rows = await _dashboard.GetBatchRowsAsync();

        rows.Count.Should().BeGreaterThan(0,
            because: "submitting a batch must add at least one row to the dashboard list");
    }

    [Test]
    [AllureStep("After submit the batch detail page shows a status and at least one item")]
    public async Task BatchDetail_AfterSubmit_StatusVisibleAndItemsListed()
    {
        var detail = await _dashboard.SubmitBatchAsync(VideoIds.EnglishManualUrl);
        await detail.WaitForLoadAsync();

        var status = await detail.GetStatusAsync();
        var itemCount = await detail.GetItemCountAsync();

        status.Should().NotBeNullOrWhiteSpace(
            because: "a submitted batch must have a visible processing status");
        itemCount.Should().BeGreaterThan(0,
            because: "the batch detail must list at least one item");
    }

    // -------------------------------------------------------------------------
    // Tests that require a completed batch (reuse the shared one from [OneTimeSetUp])
    // -------------------------------------------------------------------------

    private async Task<BatchDetailPage> NavigateToCompletedBatchAsync()
    {
        var url = PageUrl(UiRoutes.BatchDetail(_completedBatchId!));
        var detail = new BatchDetailPage(Page);
        await detail.NavigateToAsync(url);
        await detail.WaitForLoadAsync();
        return detail;
    }

    [Test]
    [AllureStep("After batch completes the transcript preview is not empty")]
    public async Task BatchDetail_AfterComplete_TranscriptPreviewNotEmpty()
    {
        var detail = await NavigateToCompletedBatchAsync();

        var preview = await detail.GetTranscriptPreviewTextAsync();

        preview.Should().NotBeNullOrWhiteSpace(
            because: "a completed transcript must display a non-empty preview");
    }

    [Test]
    [AllureStep("Export as JSON triggers a file download")]
    public async Task Export_Json_DownloadTriggered()
    {
        var detail = await NavigateToCompletedBatchAsync();

        var download = await detail.ExportAsync("json");

        download.Should().NotBeNull(
            because: "exporting as JSON must trigger a browser download");
        download.SuggestedFilename.Should().NotBeNullOrWhiteSpace(
            because: "the downloaded file must have a suggested filename");
    }

    [Test]
    [AllureStep("Export as TXT triggers a file download")]
    public async Task Export_Txt_DownloadTriggered()
    {
        var detail = await NavigateToCompletedBatchAsync();

        var download = await detail.ExportAsync("txt");

        download.Should().NotBeNull(
            because: "exporting as TXT must trigger a browser download");
        download.SuggestedFilename.Should().NotBeNullOrWhiteSpace(
            because: "the downloaded file must have a suggested filename");
    }

    [Test]
    [Ignore("SRT format is not supported by the current API")]
    [AllureStep("Export as SRT is not supported")]
    public async Task Export_Srt_IsIgnored()
    {
        var detail = await NavigateToCompletedBatchAsync();
        var download = await detail.ExportAsync("srt");
        download.Should().NotBeNull();
    }
}
