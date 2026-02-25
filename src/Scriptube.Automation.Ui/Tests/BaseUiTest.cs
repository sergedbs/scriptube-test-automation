using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Scriptube.Automation.Core.Tests;
using Scriptube.Automation.Ui.Browser;

namespace Scriptube.Automation.Ui.Tests;

/// <summary>
/// Base class for UI (Playwright) test fixtures.
/// Each test gets an isolated browser context and page; screenshots are attached to Allure on failure.
/// </summary>
public abstract class BaseUiTest : BaseTest
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Override to supply a storage-state path and restore saved auth state, skipping re-login.
    /// </summary>
    protected virtual string? StorageStatePath => null;

    [SetUp]
    public async Task SetUpBrowserAsync()
    {
        (_playwright, _browser) = await PlaywrightFactory.CreateBrowserAsync(Settings);
        Context = await BrowserContextFactory.CreateContextAsync(_browser, Settings, StorageStatePath);
        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task TearDownBrowserAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            await TakeScreenshotAsync();
        }

        await Page.CloseAsync();
        await Context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    private async Task TakeScreenshotAsync()
    {
        try
        {
            var screenshotDir = "playwright-screenshots";
            Directory.CreateDirectory(screenshotDir);

            var fileName = $"{TestContext.CurrentContext.Test.FullName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(screenshotDir, SanitizeFileName(fileName));

            var bytes = await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
            await File.WriteAllBytesAsync(filePath, bytes);

            AllureApi.AddAttachment("Screenshot on failure", "image/png", bytes, ".png");
        }
        catch
        {
            // Never let a screenshot failure break test teardown.
        }
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
