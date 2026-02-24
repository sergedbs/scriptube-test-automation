using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Shared;

/// <summary>
/// A single field-level validation error from a Pydantic 422 response.
/// Maps to the <c>ValidationError</c> OpenAPI schema.
/// </summary>
public sealed class ValidationError
{
    /// <summary>Path to the invalid field (segments are strings or array indices).</summary>
    [JsonPropertyName("loc")]
    public List<JsonElement> Loc { get; init; } = [];

    /// <summary>Human-readable error description.</summary>
    [JsonPropertyName("msg")]
    public string Msg { get; init; } = string.Empty;

    /// <summary>Pydantic error type identifier, e.g. <c>missing</c>, <c>string_type</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
