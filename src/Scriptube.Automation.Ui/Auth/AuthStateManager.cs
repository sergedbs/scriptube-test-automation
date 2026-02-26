using Microsoft.Playwright;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Ui.Navigation;

namespace Scriptube.Automation.Ui.Auth;

/// <summary>
/// Persists browser auth state (cookies + localStorage) so tests that don't cover the login
/// flow can skip re-authentication by loading the saved state via <see cref="Browser.BrowserContextFactory"/>.
/// </summary>
public static class AuthStateManager
{
    /// <summary>Path to the serialised storage state JSON (directory is gitignored).</summary>
    public static string StorageStatePath =>
        Path.Combine("playwright-screenshots", "storageState.json");

    public static bool Exists => File.Exists(StorageStatePath);

    /// <summary>
    /// Logs in using credentials from <paramref name="settings"/>, waits for the dashboard
    /// redirect, then saves the resulting context state to <see cref="StorageStatePath"/>.
    /// </summary>
    public static async Task EnsureStoredAsync(IPage page, TestSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorageStatePath)!);

        await page.GotoAsync($"{settings.BaseUrl.TrimEnd('/')}{UiRoutes.Login}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByLabel("Email").FillAsync(settings.Credentials.Email);
        await page.GetByLabel("Password").FillAsync(settings.Credentials.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await page.WaitForURLAsync($"**{UiRoutes.Dashboard}**");

        await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
        {
            Path = StorageStatePath
        });
    }
}
