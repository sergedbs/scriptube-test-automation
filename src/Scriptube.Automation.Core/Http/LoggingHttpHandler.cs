using System.Diagnostics;
using System.Text;
using Serilog;

namespace Scriptube.Automation.Core.Http;

/// <summary>
/// Delegating handler that captures every outbound request and inbound response,
/// emits them via Serilog, and makes the exchange available for Allure attachment.
/// The <c>X-API-Key</c> header value is masked in logs.
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<LoggingHttpHandler>();
    private const string MaskedValue = "***MASKED***";

    // Consumers (e.g. AllureRestLogger) may subscribe to capture exchanges.
    public event Action<HttpExchange>? OnExchange;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var requestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var maskedHeaders = MaskHeaders(request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()));

        Logger.Information(
            "→ {Method} {Url}\nHeaders: {Headers}\nBody: {Body}",
            request.Method,
            request.RequestUri,
            maskedHeaders,
            requestBody);

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
            // HttpClient uses HttpCompletionOption.ResponseContentRead by default, which fully
            // buffers the response body before returning.  ReadAsStringAsync here simply reads
            // from that internal buffer, so RestSharp can safely read Content a second time.
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "HTTP request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }

        sw.Stop();

        Logger.Information(
            "← {StatusCode} {Method} {Url} ({ElapsedMs}ms)\nBody: {Body}",
            (int)response.StatusCode,
            request.Method,
            request.RequestUri,
            sw.ElapsedMilliseconds,
            responseBody);

        var exchange = new HttpExchange(
            Method: request.Method.ToString(),
            Url: request.RequestUri?.ToString() ?? string.Empty,
            RequestHeaders: maskedHeaders,
            RequestBody: requestBody,
            StatusCode: (int)response.StatusCode,
            ResponseBody: responseBody,
            ElapsedMs: sw.ElapsedMilliseconds);

        OnExchange?.Invoke(exchange);

        return response;
    }

    private static string MaskHeaders(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var sb = new StringBuilder();
        foreach (var header in headers)
        {
            var value = string.Equals(header.Key, "X-API-Key", StringComparison.OrdinalIgnoreCase)
                ? MaskedValue
                : string.Join(", ", header.Value);
            sb.AppendLine($"  {header.Key}: {value}");
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>Immutable snapshot of a single HTTP request/response exchange.</summary>
public record HttpExchange(
    string Method,
    string Url,
    string RequestHeaders,
    string RequestBody,
    int StatusCode,
    string ResponseBody,
    long ElapsedMs);
