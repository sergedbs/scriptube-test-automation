using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>POST /api/v1/credits/precheck</c>.</summary>
public sealed class PrecheckResponse
{
    /// <summary>Total estimated credit cost across all URLs.</summary>
    [JsonPropertyName("estimated_cost")]
    public int EstimatedCost { get; init; }

    /// <summary>Per-URL breakdown of estimated costs and validation results.</summary>
    [JsonPropertyName("items")]
    public List<PrecheckItemResponse> Items { get; init; } = [];
}

/// <summary>Per-URL precheck result.</summary>
public sealed class PrecheckItemResponse
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    public string? VideoId { get; init; }

    [JsonPropertyName("estimated_cost")]
    public int EstimatedCost { get; init; }

    [JsonPropertyName("valid")]
    public bool Valid { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
