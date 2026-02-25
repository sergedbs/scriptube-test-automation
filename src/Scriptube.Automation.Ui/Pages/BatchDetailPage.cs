using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/transcripts/{batchId}</c>.</summary>
public sealed class BatchDetailPage : BasePage
{
    private ILocator StatusBadge =>
        Page.GetByTestId("batch-status")
            .Or(Page.Locator("[class*='status']").First);

    private ILocator ItemRows =>
        Page.GetByRole(AriaRole.Row)
            .Or(Page.Locator("[data-testid='transcript-item']"));

    private ILocator ExportButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Export" });

    private ILocator TranscriptPreview =>
        Page.GetByTestId("transcript-preview")
            .Or(Page.Locator("[class*='transcript']").First);

    public NavigationHeader Nav => new(Page);

    public BatchDetailPage(IPage page) : base(page) { }

    public async Task<string> GetStatusAsync() =>
        (await StatusBadge.InnerTextAsync()).Trim().ToLowerInvariant();

    public async Task<int> GetItemCountAsync() =>
        await ItemRows.CountAsync();

    public async Task<string> GetTranscriptPreviewTextAsync() =>
        await TranscriptPreview.InnerTextAsync();

    /// <summary>
    /// Polls the page until the batch status is <c>completed</c> or <c>failed</c>,
    /// or throws <see cref="TimeoutException"/> after <paramref name="timeoutMs"/> ms.
    /// </summary>
    public async Task WaitUntilCompleteAsync(int timeoutMs = 120_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        const int pollMs = 3_000;

        while (DateTime.UtcNow < deadline)
        {
            var status = await GetStatusAsync();
            if (status is "completed" or "failed")
            {
                return;
            }

            await Task.Delay(pollMs);
            await Page.ReloadAsync();
            await WaitForLoadAsync();
        }

        var finalStatus = await GetStatusAsync();
        throw new TimeoutException(
            $"Batch did not complete within {timeoutMs / 1000}s. Last status: '{finalStatus}'.");
    }

    /// <summary>
    /// Triggers an export and returns the resulting <see cref="IDownload"/>.
    /// Pass <paramref name="format"/> (e.g. "JSON") when the control is a dropdown.
    /// </summary>
    public async Task<IDownload> ExportAsync(string? format = null)
    {
        if (format is not null)
        {
            await ExportButton.ClickAsync();
            var formatOption = Page.GetByRole(AriaRole.Menuitem, new() { Name = format });
            return await Page.RunAndWaitForDownloadAsync(() => formatOption.ClickAsync());
        }

        return await Page.RunAndWaitForDownloadAsync(() => ExportButton.ClickAsync());
    }
}
