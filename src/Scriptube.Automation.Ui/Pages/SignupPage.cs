using Allure.Net.Commons;
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
    public Task SignupAsync(string email, string password) =>
        AllureApi.Step($"Sign up with email '{email}'", async () =>
        {
            await EmailInput.FillAsync(email);
            await PasswordInput.FillAsync(password);
            await SubmitButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        });

    /// <summary>Returns the error message text, or <see cref="string.Empty"/> if none appears within the configured action timeout.</summary>
    public Task<string> GetErrorMessageAsync() =>
        AllureApi.Step("Get signup error message", async () =>
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
