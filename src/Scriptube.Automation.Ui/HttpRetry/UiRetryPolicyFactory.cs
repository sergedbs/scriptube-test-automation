using Microsoft.Playwright;
using Polly;
using Polly.Retry;
using Scriptube.Automation.Core.Configuration;
using Serilog;

namespace Scriptube.Automation.Ui.HttpRetry;

/// <summary>
/// Builds Polly <see cref="ResiliencePipeline"/> instances for Playwright UI actions.
/// <para>
/// Retries on <see cref="PlaywrightException"/>, <see cref="TimeoutException"/>, and
/// <see cref="OperationCanceledException"/>. Uses exponential backoff with jitter.
/// </para>
/// </summary>
public static class UiRetryPolicyFactory
{
    private static readonly ILogger Logger = Log.ForContext(typeof(UiRetryPolicyFactory));

    /// <summary>
    /// Returns a non-generic resilience pipeline configured from <paramref name="settings"/>.
    /// </summary>
    public static ResiliencePipeline BuildPolicy(RetrySettings settings)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = settings.Count,
                Delay = TimeSpan.FromSeconds(settings.DelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<PlaywrightException>()
                    .Handle<TimeoutException>()
                    .Handle<OperationCanceledException>(),
                OnRetry = args =>
                {
                    Logger.Warning(
                        "UI action retry attempt {Attempt}/{Max}: {Reason}",
                        args.AttemptNumber + 1,
                        settings.Count,
                        args.Outcome.Exception?.Message ?? "unknown");
                    return default;
                }
            })
            .Build();
    }
}
