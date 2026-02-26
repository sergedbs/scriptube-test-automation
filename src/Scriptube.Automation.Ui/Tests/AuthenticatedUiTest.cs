using NUnit.Framework;
using Scriptube.Automation.Ui.Auth;
using Scriptube.Automation.Ui.Navigation;

namespace Scriptube.Automation.Ui.Tests;

/// <summary>
/// Base class for UI test fixtures that require a logged-in browser context.
/// Loads saved auth state on first run and performs a one-time login whenever
/// the cached state file does not yet exist or has expired.
///
/// The stale-session check (a real navigation to /ui/dashboard) runs at most once
/// per process — subsequent tests in the same run skip it via <see cref="_sessionValidated"/>.
/// </summary>
public abstract class AuthenticatedUiTest : BaseUiTest
{
    /// <summary>
    /// Set to <c>true</c> after the session is verified for the first time in a run.
    /// Prevents a full-page navigation being added to every subsequent test's setup.
    /// </summary>
    private static volatile bool _sessionValidated;

    protected override string? StorageStatePath => AuthStateManager.StorageStatePath;

    [SetUp]
    public async Task EnsureAuthStateAsync()
    {
        if (!AuthStateManager.Exists)
        {
            _sessionValidated = false;
            await AuthStateManager.EnsureStoredAsync(Page, Settings);
            _sessionValidated = true;
            return;
        }

        if (_sessionValidated)
        {
            return;
        }

        // First authenticated test in this run: navigate to the dashboard and confirm
        // the session is still live. If the server redirects to /ui/login, the saved
        // state is stale — delete it and re-authenticate.
        await Page.GotoAsync(PageUrl(UiRoutes.Dashboard));
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        if (Page.Url.Contains(UiRoutes.Login))
        {
            File.Delete(AuthStateManager.StorageStatePath);
            await AuthStateManager.EnsureStoredAsync(Page, Settings);
        }

        _sessionValidated = true;
    }
}
