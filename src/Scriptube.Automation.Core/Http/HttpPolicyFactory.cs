using Polly;
using Polly.Retry;
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
    /// Returns a typed resilience pipeline configured from <paramref name="settings"/>.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> BuildPolicy(RetrySettings settings)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = settings.Count,
                Delay = TimeSpan.FromSeconds(settings.DelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => ShouldRetryStatusCode((int)r.StatusCode)),
                OnRetry = args =>
                {
                    var reason = args.Outcome.Exception is not null
                        ? args.Outcome.Exception.Message
                        : $"HTTP {(int)args.Outcome.Result!.StatusCode}";

                    Logger.Warning(
                        "HTTP retry attempt {Attempt}/{Max}: {Reason}. Waiting {Delay}ms before next try",
                        args.AttemptNumber + 1,
                        settings.Count,
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
