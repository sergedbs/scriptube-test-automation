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

    /// <summary>
    /// Saves a full-page screenshot to <c>playwright-screenshots/</c> and returns
    /// the raw bytes for attaching to an Allure report.
    /// </summary>
    public async Task<byte[]> TakeScreenshotAsync(string name)
    {
        var dir = "playwright-screenshots";
        Directory.CreateDirectory(dir);

        var safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        var file = Path.Combine(dir, $"{safe}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

        var bytes = await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        await File.WriteAllBytesAsync(file, bytes);
        return bytes;
    }
}
