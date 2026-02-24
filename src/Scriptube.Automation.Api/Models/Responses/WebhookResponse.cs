using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Detailed webhook info from <c>GET /api/webhooks/{webhook_id}</c>.</summary>
public sealed class WebhookResponse
{
    [JsonPropertyName("webhook_id")]
    public string WebhookId { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string> Events { get; init; } = [];

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("last_triggered_at")]
    public string? LastTriggeredAt { get; init; }

    [JsonPropertyName("failure_count")]
    public int FailureCount { get; init; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;
}
