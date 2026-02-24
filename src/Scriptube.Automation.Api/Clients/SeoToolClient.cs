using RestSharp;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for the public SEO tool endpoint <c>POST /tools/youtube-transcript</c>.
/// No API key is required — <c>requiresAuth</c> is explicitly set to <c>false</c>.
/// </summary>
public sealed class SeoToolClient : ApiClientBase
{
    public SeoToolClient(TestSettings settings) : base(settings, requiresAuth: false) { }

    /// <summary>
    /// Fetches a single YouTube transcript via the public (no-auth) SEO endpoint.
    /// </summary>
    /// <param name="videoUrl">Full YouTube video URL, e.g. <c>https://www.youtube.com/watch?v=tstENMAN001</c>.</param>
    public async Task<RestResponse> GetTranscriptAsync(
        string videoUrl,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/tools/youtube-transcript", Method.Post)
            .AddJsonBody(new { url = videoUrl });
        return await ExecuteAsync(req, ct);
    }
}
