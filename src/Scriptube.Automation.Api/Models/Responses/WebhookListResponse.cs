using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/webhooks</c>.</summary>
public sealed class WebhookListResponse
{
    [JsonPropertyName("webhooks")]
    public List<WebhookResponse> Webhooks { get; init; } = [];

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
