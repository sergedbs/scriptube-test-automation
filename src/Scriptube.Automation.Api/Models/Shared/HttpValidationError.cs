using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Shared;

/// <summary>
/// Standard FastAPI/Pydantic validation error wrapper returned on HTTP 422.
/// Maps to the <c>HTTPValidationError</c> OpenAPI schema.
/// </summary>
public sealed class HttpValidationError
{
    [JsonPropertyName("detail")]
    public List<ValidationError> Detail { get; init; } = [];
}
