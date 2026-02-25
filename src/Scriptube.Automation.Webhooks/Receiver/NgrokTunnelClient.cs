using System.Text.Json;

namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Queries the ngrok local API to discover the active public HTTPS tunnel URL.
/// Requires ngrok to be running (<c>ngrok http {port}</c>) before tests start.
/// </summary>
public static class NgrokTunnelClient
{
    private const string NgrokApiUrl = "http://localhost:4040/api/tunnels";

    /// <summary>
    /// Queries <c>http://localhost:4040/api/tunnels</c> and returns the first active HTTPS tunnel URL.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// When ngrok is not running or no HTTPS tunnel is found.
    /// </exception>
    public static async Task<string> GetPublicUrlAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        string json;
        try
        {
            json = await http.GetStringAsync(NgrokApiUrl, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not reach the ngrok local API at http://localhost:4040. " +
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
