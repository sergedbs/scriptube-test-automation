using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/v1/plans</c>.</summary>
public sealed class PlansListResponse
{
    [JsonPropertyName("plans")]
    public List<PlanInfoResponse> Plans { get; init; } = [];
}
