using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Requests;

/// <summary>Request body for <c>POST /api/v1/credits/estimate</c>.</summary>
public sealed class EstimateRequest
{
    /// <summary>List of YouTube video IDs (not full URLs) to estimate cost for.</summary>
    [JsonPropertyName("video_ids")]
    public List<string> VideoIds { get; init; } = [];
}
