using Allure.Net.Commons;
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

    public Task<string> GetStatusAsync() =>
        AllureApi.Step("Get batch status", async () =>
            (await StatusBadge.InnerTextAsync()).Trim().ToLowerInvariant());

    public Task<int> GetItemCountAsync() =>
        AllureApi.Step("Get batch item count", async () =>
            int.Parse((await TotalItemsValue.InnerTextAsync()).Trim()));

    public Task<string> GetTranscriptPreviewTextAsync() =>
        AllureApi.Step("Get transcript preview text", async () =>
            (await TranscriptPreviews.First.InnerTextAsync()).Trim());

    /// <summary>
    /// Polls the page until the batch status is <c>completed</c> or <c>failed</c>,
    /// or throws <see cref="TimeoutException"/> after <paramref name="timeoutMs"/> ms.
    /// Callers should derive the timeout from <c>Settings.Timeouts.PollTimeoutSeconds * 1_000</c>.
    /// </summary>
    public Task WaitUntilCompleteAsync(int timeoutMs) =>
        AllureApi.Step($"Wait for batch to complete (timeout {timeoutMs / 1000}s)", async () =>
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            const int pollMs = 3_000;

            while (DateTime.UtcNow < deadline)
            {
                // Read status directly to avoid creating an Allure sub-step on every poll cycle.
                var status = (await StatusBadge.InnerTextAsync()).Trim().ToLowerInvariant();
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
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }

            var finalStatus = (await StatusBadge.InnerTextAsync()).Trim().ToLowerInvariant();
            throw new TimeoutException(
                $"Batch did not complete within {timeoutMs / 1000}s. Last status: '{finalStatus}'.");
        });

    /// <summary>
    /// Clicks the export link for <paramref name="format"/> and waits for the browser download.
    /// Accepts <c>"txt"</c>, <c>"csv"</c>, <c>"jsonl"</c>, or <c>"json"</c> (mapped to "jsonl").
    /// </summary>
    public Task<IDownload> ExportAsync(string? format = null)
    {
        var fmt = (format ?? "txt").ToLowerInvariant() switch
        {
            "json" => "jsonl",
            var f => f
        };

        return AllureApi.Step($"Export transcript as '{fmt}'", async () =>
        {
            var link = Page.GetByRole(AriaRole.Link, new() { Name = $"Export {fmt.ToUpper()}" });
            return await Page.RunAndWaitForDownloadAsync(() => link.ClickAsync());
        });
    }
}
