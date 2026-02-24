using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Response from <c>GET /api/v1/credits/history</c>.</summary>
public sealed class CreditHistoryResponse
{
    [JsonPropertyName("transactions")]
    public List<CreditTransactionResponse> Transactions { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }
}

/// <summary>A single credit transaction entry.</summary>
public sealed class CreditTransactionResponse
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("balance_after")]
    public int BalanceAfter { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("batch_id")]
    public string? BatchId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}
