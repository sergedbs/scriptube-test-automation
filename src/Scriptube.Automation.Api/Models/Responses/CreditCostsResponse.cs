using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/v1/credits/costs</c>.</summary>
public sealed class CreditCostsResponse
{
    /// <summary>Map of processing path name to credit cost.</summary>
    [JsonPropertyName("costs")]
    public Dictionary<string, object> Costs { get; init; } = [];
}
