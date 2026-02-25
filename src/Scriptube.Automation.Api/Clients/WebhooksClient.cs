using RestSharp;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for all <c>/api/webhooks</c> endpoints.
/// </summary>
public sealed class WebhooksClient : ApiClientBase
{
    public WebhooksClient(TestSettings settings) : base(settings) { }

    /// <summary>Registers a new webhook endpoint (<c>POST /api/webhooks/register</c>).</summary>
    public async Task<RestResponse<WebhookStatusResponse>> RegisterAsync(
        WebhookRegisterRequest request,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/register", Method.Post)
            .AddJsonBody(request);
        return await ExecuteAsync<WebhookStatusResponse>(req, ct);
    }

    /// <summary>Returns details of a specific webhook (<c>GET /api/webhooks/{webhook_id}</c>).</summary>
    public async Task<RestResponse<WebhookResponse>> GetAsync(
        string webhookId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/{webhook_id}")
            .AddUrlSegment("webhook_id", webhookId);
        return await ExecuteAsync<WebhookResponse>(req, ct);
    }

    /// <summary>Lists all registered webhooks for the authenticated user (<c>GET /api/webhooks</c>).</summary>
    public async Task<RestResponse<WebhookListResponse>> ListAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks");
        return await ExecuteAsync<WebhookListResponse>(req, ct);
    }

    /// <summary>Deletes a webhook subscription (<c>DELETE /api/webhooks/{webhook_id}</c>).</summary>
    public async Task<RestResponse<WebhookStatusResponse>> DeleteAsync(
        string webhookId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/{webhook_id}", Method.Delete)
            .AddUrlSegment("webhook_id", webhookId);
        return await ExecuteAsync<WebhookStatusResponse>(req, ct);
    }

    /// <summary>Sends a test event to a webhook to verify it is working (<c>POST /api/webhooks/{webhook_id}/test</c>).</summary>
    public async Task<RestResponse<TestEventResponse>> TriggerTestAsync(
        string webhookId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/{webhook_id}/test", Method.Post)
            .AddUrlSegment("webhook_id", webhookId);
        return await ExecuteAsync<TestEventResponse>(req, ct);
    }

    /// <summary>Returns delivery logs for a webhook (<c>GET /api/webhooks/{webhook_id}/logs</c>).</summary>
    public async Task<RestResponse<DeliveryLogsResponse>> GetLogsAsync(
        string webhookId,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/{webhook_id}/logs")
            .AddUrlSegment("webhook_id", webhookId)
            .AddQueryParameter("limit", limit)
            .AddQueryParameter("offset", offset);
        return await ExecuteAsync<DeliveryLogsResponse>(req, ct);
    }

    /// <summary>Lists all available webhook events and their descriptions (<c>GET /api/webhooks/events/available</c>).</summary>
    public async Task<RestResponse<AvailableEventsResponse>> GetAvailableEventsAsync(CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/events/available");
        return await ExecuteAsync<AvailableEventsResponse>(req, ct);
    }

    /// <summary>Retries a specific failed webhook delivery (<c>POST /api/webhooks/{webhook_id}/retry</c>).</summary>
    public async Task<RestResponse<WebhookStatusResponse>> RetryDeliveryAsync(
        string webhookId,
        string? deliveryId = null,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/webhooks/{webhook_id}/retry", Method.Post)
            .AddUrlSegment("webhook_id", webhookId);

        if (deliveryId is not null)
        {
            req.AddQueryParameter("delivery_id", deliveryId);
        }

        return await ExecuteAsync<WebhookStatusResponse>(req, ct);
    }
}
