using Polly;
using Polly.Retry;
using Polly.Timeout;
using Scriptube.Automation.Core.Configuration;
using Serilog;

namespace Scriptube.Automation.Core.Http;

/// <summary>
/// Builds Polly <see cref="ResiliencePipeline{T}"/> instances for HTTP calls.
/// <para>
/// Retry policy handles: <see cref="HttpRequestException"/>, <see cref="TaskCanceledException"/>
/// (network timeouts), and HTTP 408, 429, 5xx responses. Client errors (4xx except 408/429)
/// are never retried. Back-off uses exponential delay with jitter.
/// </para>
/// </summary>
public static class HttpPolicyFactory
{
    private static readonly ILogger Logger = Log.ForContext(typeof(HttpPolicyFactory));

    /// <summary>
    /// Returns a typed resilience pipeline configured from <paramref name="retrySettings"/> and the
    /// per-request timeout in <paramref name="requestTimeoutSeconds"/>. Timeout applies to each
    /// attempt; timeouts are retried by the subsequent retry strategy.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> BuildPolicy(
        RetrySettings retrySettings,
        int requestTimeoutSeconds)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(TimeSpan.FromSeconds(requestTimeoutSeconds))
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = retrySettings.Count,
                Delay = TimeSpan.FromSeconds(retrySettings.DelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(r => ShouldRetryStatusCode((int)r.StatusCode)),
                OnRetry = args =>
                {
                    var reason = args.Outcome.Exception is not null
                        ? args.Outcome.Exception.Message
                        : $"HTTP {(int)args.Outcome.Result!.StatusCode}";

                    Logger.Warning(
                        "HTTP retry attempt {Attempt}/{Max}: {Reason}. Waiting {Delay}ms before next try",
                        args.AttemptNumber + 1,
                        retrySettings.Count,
                        reason,
                        args.RetryDelay.TotalMilliseconds);

                    return default;
                }
            })
            .Build();
    }

    /// <summary>Returns <see langword="true"/> for status codes the policy should retry.</summary>
    internal static bool ShouldRetryStatusCode(int statusCode)
        => statusCode is 408 or 429 or >= 500;
}
