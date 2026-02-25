using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/v1/credits/balance</c>.</summary>
public sealed class CreditBalanceResponse
{
    [JsonPropertyName("credits_balance")]
    public int CreditsBalance { get; init; }

    [JsonPropertyName("plan")]
    public string Plan { get; init; } = string.Empty;

    [JsonPropertyName("daily_used")]
    public int DailyUsed { get; init; }

    [JsonPropertyName("daily_limit")]
    public int DailyLimit { get; init; }
}
