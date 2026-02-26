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

    /// <summary>Fills and submits the login form. Returns a <see cref="DashboardPage"/> after navigation.</summary>
    public async Task<DashboardPage> LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return new DashboardPage(Page);
    }

    /// <summary>Returns the error message text, or <see cref="string.Empty"/> if none is visible.</summary>
    public async Task<string> GetErrorMessageAsync()
    {
        try
        {
            await ErrorMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            return await ErrorMessage.InnerTextAsync();
        }
        catch (PlaywrightException)
        {
            return string.Empty;
        }
    }
}
