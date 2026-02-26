using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/batches/{batchId}</c>.</summary>
public sealed class BatchDetailPage : BasePage
{
    // Status badge: class="badge badge-{status}" (e.g. badge-completed, badge-processing)
    private ILocator StatusBadge => Page.Locator("span.badge").First;

    // "Total items" stat from the meta-grid
    private ILocator TotalItemsValue =>
        Page.Locator(".meta-item")
            .Filter(new() { HasText = "Total items" })
            .Locator(".meta-value");

    // Transcript text shown inline below each item
    private ILocator TranscriptPreviews => Page.Locator(".transcript-preview");

    public NavigationHeader Nav => new(Page);

    public BatchDetailPage(IPage page) : base(page) { }

    public async Task<string> GetStatusAsync() =>
        (await StatusBadge.InnerTextAsync()).Trim().ToLowerInvariant();

    public async Task<int> GetItemCountAsync() =>
        int.Parse((await TotalItemsValue.InnerTextAsync()).Trim());

    public async Task<string> GetTranscriptPreviewTextAsync() =>
        (await TranscriptPreviews.First.InnerTextAsync()).Trim();

    /// <summary>
    /// Polls the page until the batch status is <c>completed</c> or <c>failed</c>,
    /// or throws <see cref="TimeoutException"/> after <paramref name="timeoutMs"/> ms.
    /// Callers should derive the timeout from <c>Settings.Timeouts.PollTimeoutSeconds * 1_000</c>.
    /// </summary>
    public async Task WaitUntilCompleteAsync(int timeoutMs)
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
            try
            {
                // GotoAsync is safer than ReloadAsync when the server may redirect on completion.
                await Page.GotoAsync(Page.Url, new() { WaitUntil = WaitUntilState.NetworkIdle });
            }
            catch (PlaywrightException)
            {
                await WaitForLoadAsync();
            }
        }

        var finalStatus = await GetStatusAsync();
        throw new TimeoutException(
            $"Batch did not complete within {timeoutMs / 1000}s. Last status: '{finalStatus}'.");
    }

    /// <summary>
    /// Clicks the export link for <paramref name="format"/> and waits for the browser download.
    /// Accepts <c>"txt"</c>, <c>"csv"</c>, <c>"jsonl"</c>, or <c>"json"</c> (mapped to "jsonl").
    /// </summary>
    public async Task<IDownload> ExportAsync(string? format = null)
    {
        var fmt = (format ?? "txt").ToLowerInvariant() switch
        {
            "json" => "jsonl",
            var f => f
        };

        var link = Page.GetByRole(AriaRole.Link, new() { Name = $"Export {fmt.ToUpper()}" });
        return await Page.RunAndWaitForDownloadAsync(() => link.ClickAsync());
    }
}
