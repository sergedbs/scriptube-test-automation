using Scriptube.Automation.Core.Configuration;
using Serilog;

namespace Scriptube.Automation.Webhooks.Receiver;

/// <summary>
/// Manages the lifecycle of the webhook receiver used during test runs.
/// <para>
/// Two modes of operation:
/// <list type="bullet">
///   <item>
///     <b>External URL</b> — when <see cref="TestSettings.WebhookReceiverUrl"/> is non-empty the
///     value is used directly. No local server or ngrok is started.
///     <see cref="HasLocalReceiver"/> is <see langword="false"/> and <see cref="Store"/> is
///     <see langword="null"/>, so HMAC/payload assertions that require reading the raw request
///     should guard with <c>if (!HasLocalReceiver) Assert.Ignore(...)</c>.
///   </item>
///   <item>
///     <b>Local receiver + ngrok</b> — when <see cref="TestSettings.WebhookReceiverUrl"/> is
///     empty an in-process <see cref="WebhookReceiver"/> is started on
///     <see cref="TestSettings.WebhookReceiverPort"/> and the ngrok local API is queried for the
///     public HTTPS URL. <see cref="HasLocalReceiver"/> is <see langword="true"/>.
///   </item>
/// </list>
/// </para>
/// Call <see cref="StartAsync"/> once in a <c>[SetUpFixture]</c> before webhook tests run and
/// <see cref="StopAsync"/> in the corresponding <c>[OneTimeTearDown]</c>.
/// </summary>
public static class WebhookReceiverManager
{
    private static WebhookReceiver? _receiver;
    private static ReceivedRequestStore? _store;
    private static string? _activeUrl;
    private static bool _started;

    /// <summary>
    /// The HTTPS URL to register with the Scriptube webhook API, or <see langword="null"/> when
    /// no receiver is available (ngrok not running and no external URL configured).
    /// Tests that require a real receiver URL should call
    /// <see cref="BaseWebhookTest.SkipIfNoReceiverUrl"/> before using this value.
    /// </summary>
    public static string? ActiveReceiverUrl => _activeUrl;

    /// <summary>
    /// The in-process request store used to inspect received payloads and headers.
    /// <see langword="null"/> when an external URL is configured.
    /// </summary>
    public static ReceivedRequestStore? Store => _store;

    /// <summary>
    /// <see langword="true"/> when the local receiver is running and raw request data
    /// (body, headers, HMAC) can be inspected. <see langword="false"/> when an external
    /// receiver URL is being used.
    /// </summary>
    public static bool HasLocalReceiver => _store is not null;

    /// <summary>
    /// Initialises the webhook receiver based on <paramref name="settings"/>.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public static async Task StartAsync(TestSettings settings)
    {
        if (_started)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.WebhookReceiverUrl))
        {
            _activeUrl = settings.WebhookReceiverUrl;
            _started = true;

            Log.Information(
                "[WebhookReceiverManager] Using external receiver URL: {Url}",
                _activeUrl);

            return;
        }

        Log.Information(
            "[WebhookReceiverManager] Starting local receiver on port {Port}...",
            settings.WebhookReceiverPort);

        _store = new ReceivedRequestStore();
        _receiver = new WebhookReceiver(_store);
        _receiver.Start(settings.WebhookReceiverPort);

        try
        {
            _activeUrl = await NgrokTunnelClient.GetPublicUrlAsync();
            Log.Information(
                "[WebhookReceiverManager] ngrok tunnel active: {Url}",
                _activeUrl);
        }
        catch (InvalidOperationException ex)
        {
            // ngrok is not running — stop the local receiver and continue without a URL.
            // Tests that need a receiver URL will self-skip via SkipIfNoReceiverUrl().
            await _receiver.DisposeAsync();
            _receiver = null;
            _store = null;
            _activeUrl = null;
            Log.Warning(
                "[WebhookReceiverManager] ngrok not available — receiver disabled. " +
                "Tests requiring a webhook URL will be skipped. Detail: {Message}",
                ex.Message);
        }

        _started = true;
    }

    /// <summary>Stops the local receiver (if running) and resets state.</summary>
    public static async Task StopAsync()
    {
        if (_receiver is not null)
        {
            await _receiver.DisposeAsync();
            Log.Information("[WebhookReceiverManager] Local receiver stopped.");
        }

        _receiver = null;
        _store = null;
        _activeUrl = null;
        _started = false;
    }
}
