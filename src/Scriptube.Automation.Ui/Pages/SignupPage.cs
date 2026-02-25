using Microsoft.Playwright;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Page Object Model for <c>/ui/signup</c>.</summary>
public sealed class SignupPage : BasePage
{
    private ILocator EmailInput => Page.GetByLabel("Email");
    private ILocator PasswordInput => Page.GetByLabel("Password");
    private ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Create account" });
    private ILocator ErrorMessage => Page.GetByRole(AriaRole.Alert);

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
        try
        {
            await ErrorMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            return await ErrorMessage.InnerTextAsync();
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }
}
