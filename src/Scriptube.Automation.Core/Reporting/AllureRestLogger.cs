using Allure.Net.Commons;
using Scriptube.Automation.Core.Http;

namespace Scriptube.Automation.Core.Reporting;

/// <summary>
/// Subscribes to <see cref="ApiClientBase.OnExchange"/> and attaches every
/// HTTP request/response pair as an Allure step with a text attachment.
/// </summary>
public static class AllureRestLogger
{
    /// <summary>
    /// Attach the logger to <paramref name="client"/>. Call once per client instance,
    /// typically in a test fixture's <c>SetUp</c>.
    /// </summary>
    public static void Attach(ApiClientBase client)
    {
        client.OnExchange += Capture;
    }

    /// <summary>Detach the logger (e.g. in TearDown if the client is reused across tests).</summary>
    public static void Detach(ApiClientBase client)
    {
        client.OnExchange -= Capture;
    }

    private static void Capture(HttpExchange exchange)
    {
        var stepName = $"{exchange.Method} {exchange.Url} → {exchange.StatusCode}";

        AllureApi.Step(stepName, () =>
        {
            var detail = FormatExchange(exchange);
            AllureApi.AddAttachment(
                name: $"{exchange.Method} {TruncatePath(exchange.Url)}",
                type: "text/plain",
                content: System.Text.Encoding.UTF8.GetBytes(detail),
                fileExtension: ".txt");
        });
    }

    private static string FormatExchange(HttpExchange e)
    {
        return $"""
                === REQUEST ===
                {e.Method} {e.Url}
                --- Headers ---
                {e.RequestHeaders}
                --- Body ---
                {(string.IsNullOrWhiteSpace(e.RequestBody) ? "(empty)" : e.RequestBody)}

                === RESPONSE ({e.ElapsedMs}ms) ===
                Status: {e.StatusCode}
                --- Body ---
                {(string.IsNullOrWhiteSpace(e.ResponseBody) ? "(empty)" : e.ResponseBody)}
                """;
    }

    private static string TruncatePath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;
        var path = uri.AbsolutePath;
        return path.Length > 40 ? "..." + path[^37..] : path;
    }
}
