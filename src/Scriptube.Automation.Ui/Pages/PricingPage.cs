using Microsoft.Playwright;
using Scriptube.Automation.Ui.Components;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/pricing</c>.</summary>
public sealed class PricingPage : BasePage
{
    // Plan cards have class="card", contain an h2 heading and a "/month" price line.
    // This excludes FAQ cards and the "Compare Plans" / "How Credits Work" cards.
    private ILocator PlanCards =>
        Page.GetByTestId("plan-card")
            .Or(Page.Locator(".card:has(h2)").Filter(new() { HasText = "/month" }));

    public NavigationHeader Nav => new(Page);

    public PricingPage(IPage page) : base(page) { }

    /// <summary>Returns the heading text of every plan card visible on the page.</summary>
    public async Task<List<string>> GetAllPlanNamesAsync()
    {
        var cards = await PlanCards.AllAsync();
        var names = new List<string>(cards.Count);

        foreach (var card in cards)
        {
            var heading = card.Locator("h2").First;
            var name = (await heading.InnerTextAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>Returns the displayed price string for the plan whose name contains <paramref name="planName"/>.</summary>
    public async Task<string> GetPlanPriceAsync(string planName)
    {
        var cards = await PlanCards.AllAsync();

        foreach (var card in cards)
        {
            var cardText = await card.InnerTextAsync();
            if (!cardText.Contains(planName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = cardText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var priceLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith('$') || l.Contains('/'));
            return priceLine?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }
}
