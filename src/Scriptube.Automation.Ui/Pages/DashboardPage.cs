using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/dashboard</c>.</summary>
public sealed class DashboardPage : BasePage
{
    private ILocator UrlTextarea => Page.GetByRole(AriaRole.Textbox);

    private ILocator SubmitButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Extract" })
            .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }));

    private ILocator BatchListItems =>
        Page.Locator("[data-testid='batch-row']")
            .Or(Page.Locator("table tbody tr"))
            .Or(Page.Locator("[class*='batch']"));

    public NavigationHeader Nav => new(Page);

    public DashboardPage(IPage page) : base(page) { }

    /// <summary>Enters <paramref name="urls"/> (one per line), submits, and returns the resulting <see cref="BatchDetailPage"/>.</summary>
    public async Task<BatchDetailPage> SubmitBatchAsync(params string[] urls)
    {
        await UrlTextarea.ClearAsync();
        await UrlTextarea.FillAsync(string.Join("\n", urls));
        await SubmitButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return new BatchDetailPage(Page);
    }

    public async Task<IReadOnlyList<ILocator>> GetBatchRowsAsync() =>
        await BatchListItems.AllAsync();
}
