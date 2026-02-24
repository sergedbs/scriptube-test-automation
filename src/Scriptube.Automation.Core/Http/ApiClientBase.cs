using RestSharp;
using Scriptube.Automation.Core.Configuration;

namespace Scriptube.Automation.Core.Http;

/// <summary>
/// Base wrapper around <see cref="RestClient"/> that:
/// <list type="bullet">
///   <item>Injects the <c>X-API-Key</c> header on every request.</item>
///   <item>Pipes all traffic through <see cref="LoggingHttpHandler"/>.</item>
///   <item>Exposes <see cref="OnExchange"/> so Allure loggers can subscribe.</item>
/// </list>
/// </summary>
public class ApiClientBase : IDisposable
{
    protected readonly RestClient Client;
    private readonly LoggingHttpHandler _loggingHandler;
    private bool _disposed;

    /// <summary>Subscribe to receive every HTTP exchange (for Allure attachment).</summary>
    public event Action<HttpExchange>? OnExchange
    {
        add => _loggingHandler.OnExchange += value;
        remove => _loggingHandler.OnExchange -= value;
    }

    public ApiClientBase(TestSettings settings, bool requiresAuth = true)
    {
        _loggingHandler = new LoggingHttpHandler
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(_loggingHandler)
        {
            Timeout = TimeSpan.FromSeconds(settings.Timeouts.RequestSeconds)
        };

        var options = new RestClientOptions(settings.BaseUrl);

        Client = new RestClient(httpClient, options);

        if (requiresAuth && !string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            Client.AddDefaultHeader("X-API-Key", settings.ApiKey);
        }

        Client.AddDefaultHeader("Accept", "application/json");
    }

    /// <summary>Executes a request and deserialises the response body into <typeparamref name="T"/>.</summary>
    public async Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken ct = default)
        => await Client.ExecuteAsync<T>(request, ct);

    /// <summary>Executes a request and returns the raw <see cref="RestResponse"/>.</summary>
    public async Task<RestResponse> ExecuteAsync(RestRequest request, CancellationToken ct = default)
        => await Client.ExecuteAsync(request, ct);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Client.Dispose();
        _loggingHandler.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
