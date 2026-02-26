using Microsoft.Playwright;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Base class for all Page Object Models.</summary>
public abstract class BasePage
{
    protected IPage Page { get; }

    protected BasePage(IPage page) => Page = page;

    public async Task NavigateToAsync(string url)
    {
        await Page.GotoAsync(url);
        await WaitForLoadAsync();
    }

    public Task WaitForLoadAsync() =>
        Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
}
