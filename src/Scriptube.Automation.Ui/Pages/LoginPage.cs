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
    public async Task<DashboardPage> LoginAsync(string email, string password)
    {
        await SubmitFormAsync(email, password);
        await Page.WaitForURLAsync("**/ui/dashboard**");
        return new DashboardPage(Page);
    }

    /// <summary>
    /// Fills and submits the login form without waiting for any navigation.
    /// The caller is responsible for waiting on the expected outcome (error element or URL change).
    /// Use this when testing failed login scenarios (wrong password, blank fields).
    /// </summary>
    public async Task SubmitFormAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
    }

    /// <summary>Returns the error message text, or <see cref="string.Empty"/> if none appears within the configured action timeout.</summary>
    public async Task<string> GetErrorMessageAsync()
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
    }
}
