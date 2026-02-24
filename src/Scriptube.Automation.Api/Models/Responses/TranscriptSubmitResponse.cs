using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>POST /api/v1/transcripts</c> (HTTP 202).</summary>
public sealed class TranscriptSubmitResponse
{
    [JsonPropertyName("batch_id")]
    public string BatchId { get; init; } = string.Empty;

    [JsonPropertyName("batch_number")]
    public int BatchNumber { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("url_count")]
    public int UrlCount { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Indicates key source: <c>system</c> or <c>byok</c>.</summary>
    [JsonPropertyName("key_source")]
    public string KeySource { get; init; } = "system";
}
