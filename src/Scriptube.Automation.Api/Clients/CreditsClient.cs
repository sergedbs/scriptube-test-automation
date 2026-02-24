using RestSharp;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for all <c>/api/v1/credits</c> endpoints.
/// </summary>
public sealed class CreditsClient : ApiClientBase
{
    public CreditsClient(TestSettings settings) : base(settings) { }

    /// <summary>Returns the current credit balance and daily usage (<c>GET /api/v1/credits/balance</c>).</summary>
    public async Task<RestResponse<CreditBalanceResponse>> GetBalanceAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/credits/balance");
        return await ExecuteAsync<CreditBalanceResponse>(req, ct);
    }

    /// <summary>Pre-validates URLs and returns an estimated credit cost (<c>POST /api/v1/credits/precheck</c>).</summary>
    public async Task<RestResponse<PrecheckResponse>> PrecheckAsync(
        PrecheckRequest request,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/credits/precheck", Method.Post)
            .AddJsonBody(request);
        return await ExecuteAsync<PrecheckResponse>(req, ct);
    }

    /// <summary>Estimates credit cost for a list of video IDs (<c>POST /api/v1/credits/estimate</c>).</summary>
    public async Task<RestResponse<EstimateResponse>> EstimateAsync(
        EstimateRequest request,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/credits/estimate", Method.Post)
            .AddJsonBody(request);
        return await ExecuteAsync<EstimateResponse>(req, ct);
    }

    /// <summary>Returns the credit cost table for all processing paths (<c>GET /api/v1/credits/costs</c>).</summary>
    public async Task<RestResponse<CreditCostsResponse>> GetCostsAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/credits/costs");
        return await ExecuteAsync<CreditCostsResponse>(req, ct);
    }

    /// <summary>Returns the credit transaction history (<c>GET /api/v1/credits/history</c>).</summary>
    public async Task<RestResponse<CreditHistoryResponse>> GetHistoryAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/credits/history");
        return await ExecuteAsync<CreditHistoryResponse>(req, ct);
    }
}
