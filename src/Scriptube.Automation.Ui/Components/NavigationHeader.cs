using Allure.Net.Commons;
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

    // The credit-balance badge (always visible in the nav bar) links to /ui/credits.
    // The dropdown item with the same href is hidden until the dropdown is opened —
    // using the badge avoids needing to open the dropdown first.
    private ILocator CreditsLink => _page.Locator("a.credit-balance-badge[href='/ui/credits']");
    private ILocator PricingLink => _page.Locator("a.dropdown-item[href='/ui/pricing']")
        .Or(_page.Locator("a[href='/ui/pricing']"));

    private ILocator SignOutButton =>
        _page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })
             .Or(_page.GetByRole(AriaRole.Button, new() { Name = "Log out" }))
             .Or(_page.GetByRole(AriaRole.Button, new() { Name = "Logout" }));

    public NavigationHeader(IPage page) => _page = page;

    public Task<CreditsPage> ClickCreditsAsync() =>
        AllureApi.Step("Navigate to Credits page via header", async () =>
        {
            await CreditsLink.ClickAsync();
            await _page.WaitForURLAsync("**/ui/credits**");
            return new CreditsPage(_page);
        });

    public Task<PricingPage> ClickPricingAsync() =>
        AllureApi.Step("Navigate to Pricing page via header", async () =>
        {
            await PricingLink.ClickAsync();
            await _page.WaitForURLAsync("**/ui/pricing**");
            return new PricingPage(_page);
        });

    /// <summary>Clicks sign-out and waits for the redirect to <c>/ui/login</c>.</summary>
    public Task<LoginPage> SignOutAsync() =>
        AllureApi.Step("Sign out via header", async () =>
        {
            await SignOutButton.ClickAsync();
            await _page.WaitForURLAsync("**/ui/login**");
            return new LoginPage(_page);
        });
}
