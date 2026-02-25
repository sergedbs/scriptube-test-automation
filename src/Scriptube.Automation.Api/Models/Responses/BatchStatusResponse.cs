using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>
/// Full batch status returned by <c>GET /api/v1/transcripts/{batch_id}</c>
/// and listed by <c>GET /api/v1/transcripts</c>.
/// </summary>
public sealed class BatchStatusResponse
{
    [JsonPropertyName("batch_id")]
    public string BatchId { get; init; } = string.Empty;

    [JsonPropertyName("batch_number")]
    public int BatchNumber { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; init; }

    [JsonPropertyName("items")]
    public List<TranscriptItemResponse> Items { get; init; } = [];
}
