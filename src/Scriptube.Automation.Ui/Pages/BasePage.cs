using Allure.Net.Commons;
using Microsoft.Playwright;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Base class for all Page Object Models.</summary>
public abstract class BasePage
{
    protected IPage Page { get; }

    protected BasePage(IPage page) => Page = page;

    public Task NavigateToAsync(string url) =>
        AllureApi.Step($"Navigate to {url}", async () =>
        {
            await Page.GotoAsync(url);
            await WaitForLoadAsync();
        });

    public Task WaitForLoadAsync() =>
        AllureApi.Step("Wait for page load (NetworkIdle)",
            () => Page.WaitForLoadStateAsync(LoadState.NetworkIdle));
}
