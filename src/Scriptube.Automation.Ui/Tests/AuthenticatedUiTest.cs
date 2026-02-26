using NUnit.Framework;
using Scriptube.Automation.Ui.Auth;

namespace Scriptube.Automation.Ui.Tests;

/// <summary>
/// Base class for UI test fixtures that require a logged-in browser context.
/// Loads saved auth state on first run and performs a one-time login whenever
/// the cached state file does not yet exist.
/// </summary>
public abstract class AuthenticatedUiTest : BaseUiTest
{
    protected override string? StorageStatePath => AuthStateManager.StorageStatePath;

    [SetUp]
    public async Task EnsureAuthStateAsync()
    {
        if (!AuthStateManager.Exists)
        {
            await AuthStateManager.EnsureStoredAsync(Page, Settings);
        }
    }
}
