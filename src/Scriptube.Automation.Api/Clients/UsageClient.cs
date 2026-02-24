using RestSharp;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for <c>GET /api/v1/usage</c>.
/// </summary>
public sealed class UsageClient : ApiClientBase
{
    public UsageClient(TestSettings settings) : base(settings) { }

    /// <summary>Returns current usage statistics and remaining quota.</summary>
    public async Task<RestResponse<UsageResponse>> GetUsageAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/usage");
        return await ExecuteAsync<UsageResponse>(req, ct);
    }
}
