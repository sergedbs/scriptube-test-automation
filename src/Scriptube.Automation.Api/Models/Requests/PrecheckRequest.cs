using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Requests;

/// <summary>Request body for <c>POST /api/v1/credits/precheck</c>.</summary>
public sealed class PrecheckRequest
{
    /// <summary>List of YouTube URLs to pre-validate and estimate cost for.</summary>
    [JsonPropertyName("urls")]
    public List<string> Urls { get; init; } = [];
}
