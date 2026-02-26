using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.TestData;
using Scriptube.Automation.Ui.Pages;
using Scriptube.Automation.Ui.Tests;

namespace Scriptube.Automation.Tests.Ui.Regression;

/// <summary>
/// Regression tests for the batch submit → detail → export UI flow.
/// Requires an authenticated session via <see cref="AuthenticatedUiTest"/>.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("UI")]
[AllureSuite("Regression")]
[AllureFeature("Batch")]
[AllureTag("Regression", "UI", "Batch")]
public sealed class BatchTests : AuthenticatedUiTest
{
    private static readonly string VideoUrl = VideoIds.EnglishManualUrl;

    private DashboardPage _dashboard = null!;

    [SetUp]
    public async Task SetUpDashboardAsync()
    {
        _dashboard = new DashboardPage(Page);
        await _dashboard.NavigateToAsync($"{Settings.BaseUrl.TrimEnd('/')}/ui/dashboard");
        await _dashboard.WaitForLoadAsync();
    }

    [Test]
    [AllureStep("Submit a single URL — batch row appears in the dashboard list")]
    public async Task SubmitBatch_SingleUrl_BatchRowAppearsInList()
    {
        await _dashboard.SubmitBatchAsync(VideoUrl);

        // Navigate back to the dashboard to verify the newly created batch is listed.
        await _dashboard.NavigateToAsync($"{Settings.BaseUrl.TrimEnd('/')}/ui/dashboard");

        var rows = await _dashboard.GetBatchRowsAsync();

        rows.Count.Should().BeGreaterThan(0,
            because: "submitting a batch must add at least one row to the dashboard list");
    }

    [Test]
    [AllureStep("After submit the batch detail page shows a status and at least one item")]
    public async Task BatchDetail_AfterSubmit_StatusVisibleAndItemsListed()
    {
        var detail = await _dashboard.SubmitBatchAsync(VideoUrl);
        await detail.WaitForLoadAsync();

        var status = await detail.GetStatusAsync();
        var itemCount = await detail.GetItemCountAsync();

        status.Should().NotBeNullOrWhiteSpace(
            because: "a submitted batch must have a visible processing status");
        itemCount.Should().BeGreaterThan(0,
            because: "the batch detail must list at least one item");
    }

    [Test]
    [AllureStep("After batch completes the transcript preview is not empty")]
    public async Task BatchDetail_AfterComplete_TranscriptPreviewNotEmpty()
    {
        var detail = await _dashboard.SubmitBatchAsync(VideoUrl);
        await detail.WaitUntilCompleteAsync();

        var preview = await detail.GetTranscriptPreviewTextAsync();

        preview.Should().NotBeNullOrWhiteSpace(
            because: "a completed transcript must display a non-empty preview");
    }

    [Test]
    [AllureStep("Export as JSON triggers a file download")]
    public async Task Export_Json_DownloadTriggered()
    {
        var detail = await _dashboard.SubmitBatchAsync(VideoUrl);
        await detail.WaitUntilCompleteAsync();

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
        var detail = await _dashboard.SubmitBatchAsync(VideoUrl);
        await detail.WaitUntilCompleteAsync();

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
        var detail = await _dashboard.SubmitBatchAsync(VideoUrl);
        await detail.WaitUntilCompleteAsync();

        var download = await detail.ExportAsync("srt");

        download.Should().NotBeNull();
    }
}
