using RestSharp;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for <c>GET /api/v1/plans</c>.
/// </summary>
public sealed class PlansClient : ApiClientBase
{
    public PlansClient(TestSettings settings) : base(settings) { }

    /// <summary>Returns all available subscription plans and their limits.</summary>
    public async Task<RestResponse<PlansListResponse>> GetPlansAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/plans");
        return await ExecuteAsync<PlansListResponse>(req, ct);
    }
}
