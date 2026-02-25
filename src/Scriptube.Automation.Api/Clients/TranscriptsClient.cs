using RestSharp;
using Scriptube.Automation.Api.Models.Requests;
using Scriptube.Automation.Api.Models.Responses;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Api.Clients;

/// <summary>
/// Client for all <c>/api/v1/transcripts</c> endpoints.
/// </summary>
public sealed class TranscriptsClient : ApiClientBase
{
    private readonly TimeoutSettings _timeouts;

    public TranscriptsClient(TestSettings settings) : base(settings)
    {
        _timeouts = settings.Timeouts;
    }

    /// <summary>Submits a batch of YouTube URLs for transcript extraction (HTTP 202).</summary>
    public async Task<RestResponse<TranscriptSubmitResponse>> SubmitAsync(
        TranscriptRequest request,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts", Method.Post)
            .AddJsonBody(request);
        return await ExecuteAsync<TranscriptSubmitResponse>(req, ct);
    }

    /// <summary>Gets the current status and items of a batch.</summary>
    public async Task<RestResponse<BatchStatusResponse>> GetBatchAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}")
            .AddUrlSegment("batch_id", batchId);
        return await ExecuteAsync<BatchStatusResponse>(req, ct);
    }

    /// <summary>Lists all batches for the authenticated user.</summary>
    public async Task<RestResponse<List<BatchStatusResponse>>> ListBatchesAsync(
        int limit = 20,
        int offset = 0,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts")
            .AddQueryParameter("limit", limit)
            .AddQueryParameter("offset", offset);
        return await ExecuteAsync<List<BatchStatusResponse>>(req, ct);
    }

    /// <summary>
    /// Polls <c>GET /api/v1/transcripts/{batch_id}</c> until the batch status is
    /// <c>completed</c> or <c>failed</c>, or until the configured poll timeout elapses.
    /// </summary>
    /// <exception cref="TimeoutException">Thrown when the batch does not finish within the timeout.</exception>
    public async Task<BatchStatusResponse> PollUntilCompleteAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_timeouts.PollTimeoutSeconds);
        var interval = TimeSpan.FromSeconds(_timeouts.PollIntervalSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetBatchAsync(batchId, ct);
            var batch = response.Data;

            if (batch is not null)
            {
                var status = batch.Status?.ToLowerInvariant();
                if (status is "completed" or "failed" or "cancelled")
                {
                    return batch;
                }
            }

            await Task.Delay(interval, ct);
        }

        throw new TimeoutException(
            $"Batch '{batchId}' did not complete within {_timeouts.PollTimeoutSeconds}s.");
    }

    /// <summary>Exports batch results in the specified format (<c>json</c>, <c>txt</c>, or <c>srt</c>).</summary>
    public async Task<RestResponse> ExportAsync(
        string batchId,
        string format = "json",
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}/export")
            .AddUrlSegment("batch_id", batchId)
            .AddQueryParameter("format", format);
        return await ExecuteAsync(req, ct);
    }

    /// <summary>Cancels a running batch.</summary>
    public async Task<RestResponse> CancelAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}/cancel", Method.Post)
            .AddUrlSegment("batch_id", batchId);
        return await ExecuteAsync(req, ct);
    }

    /// <summary>Retries all failed items in a batch.</summary>
    public async Task<RestResponse> RetryFailedAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}/retry-failed", Method.Post)
            .AddUrlSegment("batch_id", batchId);
        return await ExecuteAsync(req, ct);
    }

    /// <summary>Reruns an entire batch from scratch.</summary>
    public async Task<RestResponse> RerunAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}/rerun", Method.Post)
            .AddUrlSegment("batch_id", batchId);
        return await ExecuteAsync(req, ct);
    }

    /// <summary>Deletes a batch permanently.</summary>
    public async Task<RestResponse> DeleteAsync(
        string batchId,
        CancellationToken ct = default)
    {
        var req = new RestRequest("/api/v1/transcripts/{batch_id}", Method.Delete)
            .AddUrlSegment("batch_id", batchId);
        return await ExecuteAsync(req, ct);
    }
}
