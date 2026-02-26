using Microsoft.Playwright;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/signup</c>.</summary>
public sealed class SignupPage : BasePage
{
    // Use ID selectors for robustness — labels are identical to the login form.
    private ILocator EmailInput => Page.Locator("#email");
    private ILocator PasswordInput => Page.Locator("#password");
    private ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Create account" });
    private ILocator ErrorMessage => Page.Locator(".alert-error");

    public SignupPage(IPage page) : base(page) { }

    /// <summary>Fills and submits the sign-up form.</summary>
    public async Task SignupAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Returns the error message text, or <see cref="string.Empty"/> if none is visible.</summary>
    public async Task<string> GetErrorMessageAsync()
    {
        if (await ErrorMessage.IsVisibleAsync())
        {
            return await ErrorMessage.InnerTextAsync();
        }

        return string.Empty;
    }
}
