using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Webhooks.Receiver;
using Serilog;

namespace Scriptube.Automation.Tests.Webhooks;

/// <summary>
/// Assembly-scoped setup fixture for the Webhook test suite.
/// Starts the in-process HTTP receiver and ngrok tunnel (or reuses the external URL from config)
/// once before any webhook test runs, and shuts it down after the last one.
/// </summary>
[SetUpFixture]
public sealed class WebhookTestSetupFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var settings = ConfigurationProvider.Get();
        await WebhookReceiverManager.StartAsync(settings);

        Log.Information(
            "[WebhookTestSetupFixture] Receiver ready. ActiveUrl={Url} HasLocalReceiver={Local}",
            WebhookReceiverManager.ActiveReceiverUrl,
            WebhookReceiverManager.HasLocalReceiver);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await WebhookReceiverManager.StopAsync();
        Log.Information("[WebhookTestSetupFixture] Receiver stopped.");
    }
}
