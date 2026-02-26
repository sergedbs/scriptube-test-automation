using System.Net;
using FluentAssertions;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="HttpPolicyFactory"/> verifying retry behaviour:
/// retriable status codes are retried up to the configured count, non-retriable
/// 4xx responses are not retried, and <see cref="HttpRequestException"/> is retried.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class HttpPolicyFactoryTests
{
    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>Builds an HttpClient that routes traffic through <see cref="RetryDelegatingHandler"/>.</summary>
    private static HttpClient BuildClient(RetrySettings settings, StubHttpHandler stub)
    {
        var policy = HttpPolicyFactory.BuildPolicy(settings);
        var retryHandler = new RetryDelegatingHandler(policy) { InnerHandler = stub };
        return new HttpClient(retryHandler);
    }

    private static RetrySettings FastRetry(int count) =>
        new() { Count = count, DelaySeconds = 0 };

    // ── retriable status code tests ─────────────────────────────────────────

    [TestCase(408)]
    [TestCase(429)]
    [TestCase(500)]
    [TestCase(502)]
    [TestCase(503)]
    public async Task HandledStatusCode_IsRetried_UntilSuccess(int statusCode)
    {
        // Arrange — two retriable failures then success
        var stub = new StubHttpHandler();
        stub.Enqueue(new HttpResponseMessage((HttpStatusCode)statusCode));
        stub.Enqueue(new HttpResponseMessage((HttpStatusCode)statusCode));
        stub.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));

        var client = BuildClient(FastRetry(count: 3), stub);

        // Act
        var response = await client.GetAsync("http://test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"HTTP {statusCode} should be retried until a success response is received");
        stub.CallCount.Should().Be(3,
            because: "two retriable failures plus the final success = 3 total calls");
    }

    [TestCase(400)]
    [TestCase(401)]
    [TestCase(403)]
    [TestCase(404)]
    [TestCase(422)]
    public async Task ClientError4xx_IsNotRetried(int statusCode)
    {
        // Arrange — a single non-retriable 4xx
        var stub = new StubHttpHandler();
        stub.Enqueue(new HttpResponseMessage((HttpStatusCode)statusCode));

        var client = BuildClient(FastRetry(count: 3), stub);

        // Act
        var response = await client.GetAsync("http://test");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)statusCode);
        stub.CallCount.Should().Be(1,
            because: $"HTTP {statusCode} is a client error and must not be retried");
    }

    [Test]
    public async Task RetryCount_IsRespected_WhenAllAttemptsRetriable()
    {
        // Arrange — policy configured for 2 retries, all responses are 503
        var stub = new StubHttpHandler(fallback: HttpStatusCode.ServiceUnavailable);
        var client = BuildClient(FastRetry(count: 2), stub);

        // Act
        var response = await client.GetAsync("http://test");

        // Assert — 1 initial + 2 retries = 3 total calls, last response returned
        stub.CallCount.Should().Be(3,
            because: "MaxRetryAttempts=2 means 1 initial attempt + 2 retries = 3 total calls");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task HttpRequestException_IsRetried()
    {
        // Arrange — first call throws, second succeeds
        var stub = new StubHttpHandler();
        stub.EnqueueException(new HttpRequestException("connection refused"));
        stub.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));

        var client = BuildClient(FastRetry(count: 2), stub);

        // Act
        var response = await client.GetAsync("http://test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.CallCount.Should().Be(2, because: "first attempt threw, second succeeded");
    }

    // ── stub helper ────────────────────────────────────────────────────────

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();
        private readonly HttpStatusCode _fallback;

        public int CallCount { get; private set; }

        public StubHttpHandler(HttpStatusCode fallback = HttpStatusCode.OK)
        {
            _fallback = fallback;
        }

        public void Enqueue(HttpResponseMessage response) =>
            _responses.Enqueue(() => response);

        public void EnqueueException(Exception ex) =>
            _responses.Enqueue(() => throw ex);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var factory = _responses.Count > 0
                ? _responses.Dequeue()
                : () => new HttpResponseMessage(_fallback);
            return Task.FromResult(factory());
        }
    }
}
