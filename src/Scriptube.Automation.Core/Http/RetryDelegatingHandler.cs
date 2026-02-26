using Polly;

namespace Scriptube.Automation.Core.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> that executes each outgoing request through a
/// Polly <see cref="ResiliencePipeline{T}"/>, enabling transparent HTTP-level retries
/// with back-off and jitter without duplicating policy setup in callers.
/// </summary>
public sealed class RetryDelegatingHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public RetryDelegatingHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
    {
        _pipeline = pipeline;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Delegate base.SendAsync via a private method: lambdas cannot call 'base' members.
        return _pipeline
            .ExecuteAsync(token => new ValueTask<HttpResponseMessage>(BaseSendAsync(request, token)), cancellationToken)
            .AsTask();
    }

    // Forwarding method that makes base.SendAsync callable from within a delegate.
    private Task<HttpResponseMessage> BaseSendAsync(HttpRequestMessage request, CancellationToken ct)
        => base.SendAsync(request, ct);
}
