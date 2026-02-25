using Microsoft.Playwright;
using Scriptube.Automation.Core.Configuration;

namespace Scriptube.Automation.Ui.Browser;

/// <summary>
/// Creates the top-level Playwright and browser instances.
/// Callers own the returned objects and must dispose them after use.
/// </summary>
public static class PlaywrightFactory
{
    /// <summary>
    /// Launches a headless Chromium browser and returns both <see cref="IPlaywright"/>
    /// and <see cref="IBrowser"/> so callers can dispose both.
    /// </summary>
    public static async Task<(IPlaywright Playwright, IBrowser Browser)> CreateBrowserAsync(
        TestSettings settings)
    {
        var playwright = await Playwright.CreateAsync();

        var browserType = settings.Browser.ToLowerInvariant() switch
        {
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => playwright.Chromium
        };

        var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        return (playwright, browser);
    }
}
