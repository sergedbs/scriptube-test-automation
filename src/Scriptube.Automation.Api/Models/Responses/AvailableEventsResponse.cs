using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/webhooks/events/available</c>.</summary>
public sealed class AvailableEventsResponse
{
    /// <summary>List of subscribable event names (e.g. <c>batch.completed</c>).</summary>
    [JsonPropertyName("events")]
    public List<string> Events { get; init; } = [];

    /// <summary>Map of event name → human-readable description.</summary>
    [JsonPropertyName("descriptions")]
    public Dictionary<string, string> Descriptions { get; init; } = [];
}
