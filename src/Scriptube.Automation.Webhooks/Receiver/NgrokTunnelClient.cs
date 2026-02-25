using System.Text.Json;

namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Queries the ngrok local API to discover the active public HTTPS tunnel URL.
/// Requires ngrok to be running (<c>ngrok http {port}</c>) before tests start.
/// </summary>
public static class NgrokTunnelClient
{
    private const string NgrokApiHost = "http://localhost";
    private const int DefaultNgrokApiPort = 4040;

    /// <summary>
    /// Queries the ngrok local API and returns the first active HTTPS tunnel URL.
    /// </summary>
    /// <param name="ngrokApiPort">Port the ngrok agent API is listening on (default: 4040).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// When ngrok is not running or no HTTPS tunnel is found.
    /// </exception>
    public static async Task<string> GetPublicUrlAsync(
        int ngrokApiPort = DefaultNgrokApiPort,
        int timeoutSeconds = 5,
        CancellationToken ct = default)
    {
        var apiUrl = $"{NgrokApiHost}:{ngrokApiPort}/api/tunnels";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };

        string json;
        try
        {
            json = await http.GetStringAsync(apiUrl, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not reach the ngrok local API at {apiUrl}. " +
                "Ensure ngrok is running: ngrok http <port>",
                ex);
        }

        using var doc = JsonDocument.Parse(json);
        var tunnels = doc.RootElement.GetProperty("tunnels");

        foreach (var tunnel in tunnels.EnumerateArray())
        {
            if (!tunnel.TryGetProperty("public_url", out var urlProp))
            {
                continue;
            }

            var url = urlProp.GetString();
            if (url?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true)
            {
                return url;
            }
        }

        throw new InvalidOperationException(
            "No active HTTPS ngrok tunnel found. " +
            "Start one with: ngrok http <port>");
    }
}
