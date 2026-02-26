using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Ui.Navigation;
using Scriptube.Automation.Ui.Pages;
using Scriptube.Automation.Ui.Tests;

namespace Scriptube.Automation.Tests.Ui.Regression;

/// <summary>
/// Regression tests for authentication flows (login and signup).
/// Extends <see cref="BaseUiTest"/> directly — these tests exercise the auth UI itself.
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("UI")]
[AllureSuite("Regression")]
[AllureFeature("Auth")]
[AllureTag("Regression", "UI", "Auth")]
public sealed class AuthTests : BaseUiTest
{
    [TearDown]
    public async Task CoolDownAsync()
    {
        // The login endpoint is nginx-rate-limited. A brief pause between tests
        // prevents the 503 "Service Temporarily Unavailable" that fires when three
        // login attempts land in rapid succession.
        await Task.Delay(Settings.Timeouts.AuthCooldownMs);
    }
    [Test]
    [AllureStep("Login with valid credentials redirects to dashboard")]
    public async Task Login_ValidCredentials_RedirectsToDashboard()
    {
        var login = new LoginPage(Page);
        await login.NavigateToAsync(PageUrl(UiRoutes.Login));
        await login.WaitForLoadAsync();

        var dashboard = await login.LoginAsync(Settings.Credentials.Email, Settings.Credentials.Password);

        await dashboard.WaitForLoadAsync();
        Page.Url.Should().Contain(UiRoutes.Dashboard,
            because: "valid credentials must redirect to the dashboard");
    }

    [Test]
    [AllureStep("Login with wrong password shows an error message")]
    public async Task Login_WrongPassword_ShowsError()
    {
        var login = new LoginPage(Page);
        await login.NavigateToAsync(PageUrl(UiRoutes.Login));
        await login.WaitForLoadAsync();

        // Use SubmitFormAsync — does not assume a dashboard redirect.
        await login.SubmitFormAsync(Settings.Credentials.Email, "wrong-password-that-does-not-exist");

        Page.Url.Should().Contain(UiRoutes.Login,
            because: "invalid credentials must not redirect to the dashboard");
        var error = await login.GetErrorMessageAsync();
        error.Should().NotBeNullOrWhiteSpace(
            because: "an incorrect password must display an inline error message");
    }

    [Test]
    [AllureStep("Login with blank email shows a validation message")]
    public async Task Login_BlankEmail_ShowsValidation()
    {
        var login = new LoginPage(Page);
        await login.NavigateToAsync(PageUrl(UiRoutes.Login));
        await login.WaitForLoadAsync();

        // Blank email triggers HTML5 browser validation, which blocks form submission.
        // The page stays on /ui/login — assert the URL did not change.
        await login.SubmitFormAsync(string.Empty, Settings.Credentials.Password);

        Page.Url.Should().Contain(UiRoutes.Login,
            because: "submitting with a blank email must keep the user on the login page");
    }

    [Test]
    [Ignore("UI shows a check-inbox flow for all emails including duplicates — no reliable DOM signal to assert against")]
    [AllureStep("Signup with a duplicate email shows an error")]
    public async Task Signup_DuplicateEmail_ShowsError()
    {
        var signup = new SignupPage(Page);
        await signup.NavigateToAsync(PageUrl(UiRoutes.Signup));
        await signup.WaitForLoadAsync();

        await signup.SignupAsync(Settings.Credentials.Email, Settings.Credentials.Password);
        var error = await signup.GetErrorMessageAsync();

        error.Should().NotBeNullOrWhiteSpace(
            because: "registering with an already-used email must display an error");
    }

    [Test]
    [Ignore("Creates a real account — run manually against a disposable environment only")]
    [AllureStep("Signup with a new email creates an account and redirects")]
    public async Task Signup_NewEmail_CreatesAccount()
    {
        var uniqueEmail = $"test+{Guid.NewGuid():N}@example.com";
        var signup = new SignupPage(Page);
        await signup.NavigateToAsync(PageUrl(UiRoutes.Signup));
        await signup.WaitForLoadAsync();

        await signup.SignupAsync(uniqueEmail, "Test1234!");

        Page.Url.Should().NotContain(UiRoutes.Signup,
            because: "successful registration must navigate away from the signup page");
    }
}
