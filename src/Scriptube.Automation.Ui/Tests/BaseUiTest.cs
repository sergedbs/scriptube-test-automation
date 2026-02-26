using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Scriptube.Automation.Core.Tests;
using Scriptube.Automation.Ui.Browser;
using Scriptube.Automation.Ui.Navigation;
using Scriptube.Automation.Ui.Pages;

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

    /// <summary>
    /// Builds a full page URL by appending a relative <paramref name="route"/> to <see cref="BaseTest.Settings"/>.<c>BaseUrl</c>.
    /// Use with constants from <see cref="UiRoutes"/>.
    /// </summary>
    protected string PageUrl(string route) =>
        $"{Settings.BaseUrl.TrimEnd('/')}{route}";

    /// <summary>
    /// Waits for <paramref name="page"/> to reach a terminal batch status, using the
    /// poll timeout from <see cref="BaseTest.Settings"/>.
    /// </summary>
    protected Task WaitForBatchAsync(BatchDetailPage page) =>
        page.WaitUntilCompleteAsync(Settings.Timeouts.PollTimeoutSeconds * 1_000);

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

        if (Page is not null)
        {
            try { await Page.CloseAsync(); } catch { /* ignored */ }
        }

        if (Context is not null)
        {
            try { await Context.CloseAsync(); } catch { /* ignored */ }
        }

        if (_browser is not null)
        {
            try { await _browser.CloseAsync(); } catch { /* ignored */ }
        }

        if (_playwright is not null)
        {
            try { _playwright.Dispose(); } catch { /* ignored */ }
        }
    }

    private async Task TakeScreenshotAsync()
    {
        try
        {
            var fileName = $"{TestContext.CurrentContext.Test.FullName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(Path.GetTempPath(), "scriptube-playwright-screens", SanitizeFileName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var bytes = await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
            await File.WriteAllBytesAsync(filePath, bytes);

            AllureApi.AddAttachment("Screenshot on failure", "image/png", bytes, ".png");

            // Avoid unbounded disk growth on long-running agents.
            try { File.Delete(filePath); } catch { /* ignored */ }
        }
        catch
        {
            // Never let a screenshot failure break test teardown.
        }
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
