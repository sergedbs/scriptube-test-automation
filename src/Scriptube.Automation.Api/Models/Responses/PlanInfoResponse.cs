using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Responses;

/// <summary>Details of a single subscription plan from <c>GET /api/v1/plans</c>.</summary>
public sealed class PlanInfoResponse
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("price_monthly_cents")]
    public int PriceMonthlyCents { get; init; }

    [JsonPropertyName("price_annual_cents")]
    public int PriceAnnualCents { get; init; }

    [JsonPropertyName("videos_per_day")]
    public int VideosPerDay { get; init; }

    [JsonPropertyName("max_batch_size")]
    public int MaxBatchSize { get; init; }

    [JsonPropertyName("concurrent_batches")]
    public int ConcurrentBatches { get; init; }

    [JsonPropertyName("features")]
    public List<string> Features { get; init; } = [];
}
