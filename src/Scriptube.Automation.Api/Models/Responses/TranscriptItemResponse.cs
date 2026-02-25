using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>A single video item inside a <see cref="BatchStatusResponse"/>.</summary>
public sealed class TranscriptItemResponse
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("channel")]
    public string? Channel { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("transcript_text")]
    public string? TranscriptText { get; init; }

    [JsonPropertyName("transcript_language")]
    public string? TranscriptLanguage { get; init; }

    [JsonPropertyName("duration_seconds")]
    public int? DurationSeconds { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
