using Microsoft.Playwright;
using Scriptube.Automation.Core.Configuration;

namespace Scriptube.Automation.Ui.Browser;

/// <summary>
/// Creates <see cref="IBrowserContext"/> instances with standardised viewport, locale, and timeout options.
/// Supply <paramref name="storageStatePath"/> to restore saved auth state and skip re-login.
/// </summary>
public static class BrowserContextFactory
{
    public static async Task<IBrowserContext> CreateContextAsync(
        IBrowser browser,
        TestSettings settings,
        string? storageStatePath = null)
    {
        var options = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = settings.ViewportWidth,
                Height = settings.ViewportHeight
            },
            Locale = "en-US"
        };

        if (storageStatePath is not null && File.Exists(storageStatePath))
        {
            options.StorageStatePath = storageStatePath;
        }

        var context = await browser.NewContextAsync(options);
        context.SetDefaultNavigationTimeout(settings.Timeouts.PlaywrightNavigationMs);
        context.SetDefaultTimeout(settings.Timeouts.PlaywrightActionMs);
        return context;
    }
}
