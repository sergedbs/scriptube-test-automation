namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Immutable snapshot of a single inbound HTTP request captured by the local webhook receiver.
/// </summary>
/// <param name="Body">Raw UTF-8 request body.</param>
/// <param name="Headers">Request headers, keyed case-insensitively.</param>
/// <param name="ReceivedAt">UTC timestamp when the request arrived.</param>
public sealed record ReceivedRequest(
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ReceivedAt);
