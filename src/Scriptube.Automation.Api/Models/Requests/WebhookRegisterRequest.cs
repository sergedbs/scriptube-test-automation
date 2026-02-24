using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Requests;

/// <summary>
/// Request body for <c>POST /api/webhooks/register</c>.
/// Maps to the <c>WebhookRegisterRequest</c> OpenAPI schema.
/// </summary>
public sealed class WebhookRegisterRequest
{
    /// <summary>The HTTPS URL that will receive webhook POST requests.</summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// One or more event names to subscribe to, e.g.
    /// <c>batch.completed</c>, <c>transcript.ready</c>, <c>credits.low</c>.
    /// At least one event is required.
    /// </summary>
    [JsonPropertyName("events")]
    public List<string> Events { get; init; } = [];

    /// <summary>
    /// Secret used for HMAC-SHA256 payload signing (16–256 characters).
    /// The <c>X-Scriptube-Signature</c> header is computed with this value.
    /// </summary>
    [JsonPropertyName("secret")]
    public string Secret { get; init; } = string.Empty;
}
