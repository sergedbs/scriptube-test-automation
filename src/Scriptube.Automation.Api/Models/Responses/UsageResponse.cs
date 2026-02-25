using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/v1/usage</c>.</summary>
public sealed class UsageResponse
{
    [JsonPropertyName("plan")]
    public string Plan { get; init; } = string.Empty;

    [JsonPropertyName("daily_used")]
    public int DailyUsed { get; init; }

    [JsonPropertyName("daily_limit")]
    public int DailyLimit { get; init; }

    [JsonPropertyName("daily_remaining")]
    public int DailyRemaining { get; init; }

    [JsonPropertyName("monthly_used")]
    public int MonthlyUsed { get; init; }

    [JsonPropertyName("total_processed")]
    public int TotalProcessed { get; init; }
}
