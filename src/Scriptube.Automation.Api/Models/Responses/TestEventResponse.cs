using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>POST /api/webhooks/{webhook_id}/test</c>.</summary>
public sealed class TestEventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("delivery_id")]
    public string DeliveryId { get; init; } = string.Empty;

    [JsonPropertyName("response_code")]
    public int? ResponseCode { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
