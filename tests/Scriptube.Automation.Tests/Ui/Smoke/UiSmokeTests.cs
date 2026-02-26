using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Ui.Navigation;
using Scriptube.Automation.Ui.Pages;
using Scriptube.Automation.Ui.Tests;

namespace Scriptube.Automation.Tests.Ui.Smoke;

/// <summary>
/// Smoke tests for core UI pages that require an authenticated session.
/// Verifies pages load and return expected content.
/// </summary>
[TestFixture]
[Category("Smoke")]
[Category("UI")]
[AllureSuite("Smoke")]
[AllureFeature("UI")]
[AllureTag("Smoke", "UI")]
public sealed class UiSmokeTests : AuthenticatedUiTest
{
    [Test]
    [AllureStep("Credits page loads with a non-negative balance and at least one credit pack")]
    public async Task CreditsPage_LoadsWithBalance()
    {
        var credits = new CreditsPage(Page);
        await credits.NavigateToAsync(PageUrl(UiRoutes.Credits));
        await credits.WaitForLoadAsync();

        var balance = await credits.GetDisplayedBalanceAsync();
        var packCount = await credits.GetCreditPackCountAsync();

        balance.Should().BeGreaterThanOrEqualTo(0,
            because: "credits balance must be a non-negative number");
        packCount.Should().BeGreaterThan(0,
            because: "at least one credit pack must be visible on the credits page");
    }

    [Test]
    [AllureStep("Pricing page loads and shows at least two plans with a non-empty Pro price")]
    public async Task PricingPage_ShowsAtLeastTwoPlans()
    {
        var pricing = new PricingPage(Page);
        await pricing.NavigateToAsync(PageUrl(UiRoutes.Pricing));
        await pricing.WaitForLoadAsync();

        var plans = await pricing.GetAllPlanNamesAsync();
        var proPrice = await pricing.GetPlanPriceAsync("Pro");

        plans.Count.Should().BeGreaterThanOrEqualTo(2,
            because: "the pricing page must list at least two subscription plans");
        proPrice.Should().NotBeNullOrWhiteSpace(
            because: "the Pro plan must display a price");
    }

    [Test]
    [AllureStep("Navigation header Credits link navigates to the credits page")]
    public async Task NavigationHeader_CreditsLink_NavigatesToCreditsPage()
    {
        // Start on the dashboard so the authenticated navigation header is present.
        var dashboard = new DashboardPage(Page);
        await dashboard.NavigateToAsync(PageUrl(UiRoutes.Dashboard));
        await dashboard.WaitForLoadAsync();

        var credits = await dashboard.Nav.ClickCreditsAsync();
        await credits.WaitForLoadAsync();

        Page.Url.Should().Contain(UiRoutes.Credits,
            because: "clicking the Credits nav link must navigate to the credits page");
    }
}
