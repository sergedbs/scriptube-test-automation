using Allure.Net.Commons;
using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/dashboard</c>.</summary>
public sealed class DashboardPage : BasePage
{
    private ILocator UrlTextarea => Page.Locator("#urls");

    private ILocator SubmitButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Get Transcripts" });

    private ILocator BatchListItems => Page.Locator("tr[data-batch-id]");

    public NavigationHeader Nav => new(Page);

    public DashboardPage(IPage page) : base(page) { }

    /// <summary>Enters <paramref name="urls"/> (one per line), submits, confirms the cost modal, and returns the resulting <see cref="BatchDetailPage"/>.</summary>
    public Task<BatchDetailPage> SubmitBatchAsync(params string[] urls) =>
        AllureApi.Step($"Submit batch with {urls.Length} URL(s)", async () =>
        {
            await UrlTextarea.ClearAsync();
            await UrlTextarea.FillAsync(string.Join("\n", urls));
            await SubmitButton.ClickAsync();

            // A credit-cost confirmation modal appears before the form is submitted.
            var confirmButton = Page.GetByRole(AriaRole.Button, new() { Name = "Confirm & Process" });
            await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await confirmButton.ClickAsync();

            await Page.WaitForURLAsync("**/ui/batches/**");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            return new BatchDetailPage(Page);
        });

    public Task<IReadOnlyList<ILocator>> GetBatchRowsAsync() =>
        AllureApi.Step("Get batch list rows",
            () => BatchListItems.AllAsync());
}
