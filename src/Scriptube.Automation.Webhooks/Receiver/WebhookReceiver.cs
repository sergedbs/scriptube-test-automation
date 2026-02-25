using System.Net;
using System.Text;

namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Lightweight in-process HTTP server that captures inbound webhook deliveries.
/// Built on <see cref="HttpListener"/> — no ASP.NET dependency required.
/// </summary>
public sealed class WebhookReceiver : IAsyncDisposable
{
    private readonly ReceivedRequestStore _store;
    private HttpListener? _listener;
    private Task? _acceptLoop;
    private CancellationTokenSource? _cts;

    public WebhookReceiver(ReceivedRequestStore store)
    {
        _store = store;
    }

    /// <summary>Starts the listener on <c>http://*:{port}/</c> and begins the accept loop.</summary>
    /// <remarks>
    /// Using <c>*</c> (any host) rather than <c>localhost</c> ensures the listener accepts
    /// requests forwarded by ngrok, which preserves the original <c>Host</c> header
    /// (e.g. <c>xxxx.ngrok-free.app</c>) instead of <c>localhost</c>.
    /// </remarks>
    public void Start(int port)
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://*:{port}/");
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener!.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Listener was stopped externally — exit gracefully.
                break;
            }

            // Handle each request on the thread-pool without blocking the accept loop.
            _ = Task.Run(() => HandleRequestAsync(ctx), CancellationToken.None);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        try
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in ctx.Request.Headers.Keys)
            {
                headers[key] = ctx.Request.Headers[key] ?? string.Empty;
            }

            _store.Enqueue(new ReceivedRequest(body, headers, DateTimeOffset.UtcNow));

            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        }
        catch
        {
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
            catch { /* ignore */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        if (_acceptLoop is not null)
        {
            try { await _acceptLoop; }
            catch { /* cancellation or listener stop — both expected */ }
        }

        _listener?.Stop();
        _listener?.Close();
        _cts?.Dispose();
    }
}
