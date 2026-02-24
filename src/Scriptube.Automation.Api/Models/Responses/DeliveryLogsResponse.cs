using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/webhooks/{webhook_id}/logs</c>.</summary>
public sealed class DeliveryLogsResponse
{
    [JsonPropertyName("deliveries")]
    public List<DeliveryLogResponse> Deliveries { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }
}

/// <summary>A single webhook delivery log entry.</summary>
public sealed class DeliveryLogResponse
{
    [JsonPropertyName("delivery_id")]
    public string DeliveryId { get; init; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("response_code")]
    public int? ResponseCode { get; init; }

    [JsonPropertyName("attempts")]
    public int Attempts { get; init; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("next_retry_at")]
    public string? NextRetryAt { get; init; }
}
