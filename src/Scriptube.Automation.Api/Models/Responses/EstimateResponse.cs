using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>POST /api/v1/credits/estimate</c>.</summary>
public sealed class EstimateResponse
{
    /// <summary>Total estimated credit cost across all video IDs.</summary>
    [JsonPropertyName("estimated_cost")]
    public int EstimatedCost { get; init; }

    /// <summary>Per-video-ID cost breakdown.</summary>
    [JsonPropertyName("items")]
    public List<EstimateItemResponse> Items { get; init; } = [];
}

/// <summary>Per-video estimate item.</summary>
public sealed class EstimateItemResponse
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("estimated_cost")]
    public int EstimatedCost { get; init; }

    [JsonPropertyName("processing_path")]
    public string? ProcessingPath { get; init; }
}
