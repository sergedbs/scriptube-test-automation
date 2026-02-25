using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>
/// Status response returned by register, delete, and retry webhook operations.
/// </summary>
public sealed class WebhookStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("webhook_id")]
    public string? WebhookId { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
