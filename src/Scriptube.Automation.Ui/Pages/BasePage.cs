using Allure.Net.Commons;
using Microsoft.Playwright;
using Polly;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Ui.HttpRetry;
using Serilog;

namespace Scriptube.Automation.Ui.Pages;

/// <summary>Base class for all Page Object Models.</summary>
public abstract class BasePage
{
    protected IPage Page { get; }
    private readonly ResiliencePipeline _retryPolicy;
    private static readonly ILogger Logger = Log.ForContext<BasePage>();

    /// <param name="page">The Playwright page instance for this page object.</param>
    /// <param name="retry">
    /// Optional retry settings. When provided, <see cref="NavigateToAsync"/>,
    /// <see cref="WaitForLoadAsync"/>, and <see cref="ExecuteWithRetryAsync"/> will
    /// retry on <see cref="PlaywrightException"/>, <see cref="TimeoutException"/>, or
    /// <see cref="OperationCanceledException"/>. Pass <see langword="null"/> (default)
    /// to run without retries.
    /// </param>
    protected BasePage(IPage page, RetrySettings? retry = null)
    {
        Page = page;
        _retryPolicy = retry is null
            ? ResiliencePipeline.Empty
            : UiRetryPolicyFactory.BuildPolicy(retry);
    }

    /// <summary>Navigates to <paramref name="url"/> and waits for the page to reach NetworkIdle.</summary>
    public Task NavigateToAsync(string url) =>
        AllureApi.Step($"Navigate to {url}", async () =>
        {
            Logger.Debug("UI navigate → {Url}", url);
            await ExecuteWithRetryAsync(() => Page.GotoAsync(url));
            await WaitForLoadAsync();
        });

    /// <summary>Waits for the page to reach <see cref="LoadState.NetworkIdle"/>.</summary>
    public Task WaitForLoadAsync() =>
        AllureApi.Step("Wait for page load (NetworkIdle)", async () =>
        {
            await ExecuteWithRetryAsync(() => Page.WaitForLoadStateAsync(LoadState.NetworkIdle));
            Logger.Debug("UI page load complete (NetworkIdle)");
        });

    /// <summary>
    /// Executes <paramref name="action"/> through the configured UI retry policy.
    /// Retries occur on <see cref="PlaywrightException"/>, <see cref="TimeoutException"/>,
    /// and <see cref="OperationCanceledException"/> when retry settings were supplied.
    /// <para>
    /// Do not pass credential values (<c>password</c>) as part of a log message or description
    /// string — only the exception message is logged on retry.
    /// </para>
    /// </summary>
    protected Task ExecuteWithRetryAsync(Func<Task> action) =>
        _retryPolicy
            .ExecuteAsync(_ => new ValueTask(action()), CancellationToken.None)
            .AsTask();
}
