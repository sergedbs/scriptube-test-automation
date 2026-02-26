namespace Scriptube.Automation.Ui.Navigation;

/// <summary>
/// Relative URL paths for all Scriptube UI pages.
/// Combine with <c>Settings.BaseUrl</c> via <c>BaseUiTest.PageUrl(route)</c>.
/// </summary>
public static class UiRoutes
{
    public const string Login = "/ui/login";
    public const string Signup = "/ui/signup";
    public const string Dashboard = "/ui/dashboard";
    public const string Credits = "/ui/credits";
    public const string Pricing = "/ui/pricing";
    public const string ApiKeys = "/ui/api-keys";

    /// <summary>Returns the route for a specific batch detail page.</summary>
    public static string BatchDetail(string batchId) => $"/ui/batches/{batchId}";
}
