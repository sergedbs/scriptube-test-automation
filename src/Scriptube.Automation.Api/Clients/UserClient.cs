using RestSharp;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for <c>GET /api/v1/user</c>.
/// </summary>
public sealed class UserClient : ApiClientBase
{
    public UserClient(TestSettings settings) : base(settings) { }

    /// <summary>Returns the authenticated user's profile and plan details.</summary>
    public async Task<RestResponse<UserInfoResponse>> GetUserAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/user");
        return await ExecuteAsync<UserInfoResponse>(req, ct);
    }
}
