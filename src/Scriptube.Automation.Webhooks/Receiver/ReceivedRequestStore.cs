using System.Collections.Concurrent;

namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Thread-safe store for inbound webhook requests captured by the local receiver.
/// Consumed by tests that need to inspect webhook payloads and HMAC signatures.
/// </summary>
public sealed class ReceivedRequestStore
{
    private readonly ConcurrentQueue<ReceivedRequest> _queue = new();

    /// <summary>Enqueues an inbound request. Called from the receiver background loop.</summary>
    public void Enqueue(ReceivedRequest request) => _queue.Enqueue(request);

    /// <summary>Discards all queued requests. Call this in test <c>[SetUp]</c> for isolation.</summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Waits until a request arrives in the queue or <paramref name="timeout"/> elapses.
    /// Polls every 200 ms.
    /// </summary>
    /// <exception cref="TimeoutException">When no request arrives within the timeout.</exception>
    public async Task<ReceivedRequest> WaitForRequestAsync(
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_queue.TryDequeue(out var request))
            {
                return request;
            }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException(
            $"No webhook request was received within {timeout.TotalSeconds:F0} second(s).");
    }
}
