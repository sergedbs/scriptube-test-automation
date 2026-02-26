using Allure.Net.Commons;
using Microsoft.Playwright;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/login</c>.</summary>
public sealed class LoginPage : BasePage
{
    private ILocator EmailInput => Page.GetByLabel("Email");
    private ILocator PasswordInput => Page.GetByLabel("Password");
    private ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
    private ILocator ErrorMessage => Page.Locator(".alert-error");

    public LoginPage(IPage page) : base(page) { }

    /// <summary>
    /// Fills and submits the login form, then waits for the dashboard URL.
    /// Use this on the happy path only — it asserts an implicit redirect to <c>/ui/dashboard</c>.
    /// </summary>
    public Task<DashboardPage> LoginAsync(string email, string password) =>
        AllureApi.Step($"Login with email '{email}'", async () =>
        {
            await SubmitFormAsync(email, password);
            await Page.WaitForURLAsync("**/ui/dashboard**");
            return new DashboardPage(Page);
        });

    /// <summary>
    /// Fills and submits the login form without waiting for any navigation.
    /// The caller is responsible for waiting on the expected outcome (error element or URL change).
    /// Use this when testing failed login scenarios (wrong password, blank fields).
    /// </summary>
    public Task SubmitFormAsync(string email, string password) =>
        AllureApi.Step($"Submit login form with email '{email}'", async () =>
        {
            await EmailInput.FillAsync(email);
            await PasswordInput.FillAsync(password);
            await SubmitButton.ClickAsync();
            // Wait for the server response to process — ensures the error element
            // (or redirect) is in the DOM before the caller inspects the page.
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        });

    /// <summary>Returns the error message text, or <see cref="string.Empty"/> if none appears within the configured action timeout.</summary>
    public Task<string> GetErrorMessageAsync() =>
        AllureApi.Step("Get login error message", async () =>
        {
            try
            {
                await ErrorMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                return await ErrorMessage.InnerTextAsync();
            }
            catch (PlaywrightException)
            {
                return string.Empty;
            }
        });
}
