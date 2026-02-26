using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/credits</c>.</summary>
public sealed class CreditsPage : BasePage
{
    private ILocator BalanceDisplay =>
        Page.GetByTestId("credits-balance")
            .Or(Page.Locator("[class*='balance']").First);

    private ILocator CreditPackCards =>
        Page.GetByTestId("credit-pack")
            .Or(Page.Locator("[class*='pack']"));

    public NavigationHeader Nav => new(Page);

    public CreditsPage(IPage page) : base(page) { }

    /// <summary>Reads and parses the displayed credit balance as a whole number.</summary>
    public async Task<int> GetDisplayedBalanceAsync()
    {
        var raw = (await BalanceDisplay.InnerTextAsync()).Trim();
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int> GetCreditPackCountAsync() =>
        await CreditPackCards.CountAsync();
}
