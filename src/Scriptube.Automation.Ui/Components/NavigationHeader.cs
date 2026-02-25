using Microsoft.Playwright;
using Scriptube.Automation.Ui.Pages;

namespace Scriptube.Automation.Ui.Components;

/// <summary>
/// Navigation header component shared across all authenticated pages.
/// Instantiate directly: <c>new NavigationHeader(Page)</c>.
/// </summary>
public sealed class NavigationHeader
{
    private readonly IPage _page;

    private ILocator CreditsLink =>
        _page.GetByRole(AriaRole.Link, new() { Name = "Credits" })
             .Or(_page.GetByRole(AriaRole.Link, new() { Name = "credits" }));

    private ILocator PricingLink =>
        _page.GetByRole(AriaRole.Link, new() { Name = "Pricing" })
             .Or(_page.GetByRole(AriaRole.Link, new() { Name = "pricing" }));

    private ILocator SignOutButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })
             .Or(_page.GetByRole(AriaRole.Button, new() { Name = "Log out" }))
             .Or(_page.GetByRole(AriaRole.Button, new() { Name = "Logout" }));

    public NavigationHeader(IPage page) => _page = page;

    public async Task<CreditsPage> ClickCreditsAsync()
    {
        await CreditsLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return new CreditsPage(_page);
    }

    public async Task<PricingPage> ClickPricingAsync()
    {
        await PricingLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return new PricingPage(_page);
    }

    /// <summary>Clicks sign-out and waits for the redirect to <c>/ui/login</c>.</summary>
    public async Task<LoginPage> SignOutAsync()
    {
        await SignOutButton.ClickAsync();
        await _page.WaitForURLAsync("**/ui/login**");
        return new LoginPage(_page);
    }
}
