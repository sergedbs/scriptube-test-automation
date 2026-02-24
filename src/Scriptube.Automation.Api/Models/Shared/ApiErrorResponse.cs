using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Shared;

/// <summary>
/// Generic error envelope for non-422 error responses (4xx / 5xx).
/// Not formally in the OpenAPI spec but matches common FastAPI error shapes.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>Error code or message string returned by the API.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>Optional structured message (some endpoints return an object).</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Optional machine-readable error code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }
}
