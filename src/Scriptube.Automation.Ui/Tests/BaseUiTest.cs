using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Scriptube.Automation.Core.Tests;

namespace Scriptube.Automation.Ui.Tests;

/// <summary>
/// Base class for UI (Playwright) test fixtures.
/// NUnit calls every [SetUp]/[TearDown] in the hierarchy automatically (base → derived).
/// So <see cref="BaseTest.SetUp"/> runs first, then <see cref="SetUpBrowserAsync"/>.
/// Each test gets an isolated <see cref="IBrowserContext"/> and <see cref="IPage"/>.
/// Screenshots are taken on failure and attached to the Allure report.
/// </summary>
public abstract class BaseUiTest : BaseTest
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    /// <summary>Called by NUnit after <see cref="BaseTest.SetUp"/> completes.</summary>
    [SetUp]
    public async Task SetUpBrowserAsync()
    {
        _playwright = await Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        Context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Settings.ViewportWidth, Height = Settings.ViewportHeight },
            Locale = "en-US"
        });

        Context.SetDefaultNavigationTimeout(Settings.Timeouts.PlaywrightNavigationMs);
        Context.SetDefaultTimeout(Settings.Timeouts.PlaywrightActionMs);

        Page = await Context.NewPageAsync();
    }

    /// <summary>Called by NUnit before <see cref="BaseTest.TearDown"/> completes.</summary>
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
            // Never let screenshot failure break the test teardown.
        }
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
