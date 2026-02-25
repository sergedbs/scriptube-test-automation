using NUnit.Framework;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Core.Http;
using Scriptube.Automation.Core.Reporting;
using Scriptube.Automation.Core.Tests;
using Scriptube.Automation.Webhooks.Receiver;

namespace Scriptube.Automation.Webhooks.Tests;

/// <summary>
/// Base class for webhook test fixtures.
/// Provides typed API clients (<see cref="Webhooks"/>, <see cref="Transcripts"/>)
/// with Allure logging and convenience access to the webhook receiver managed by
/// <see cref="WebhookReceiverManager"/>.
/// </summary>
public abstract class BaseWebhookTest : BaseTest
{
    /// <summary>Typed client for all <c>/api/webhooks</c> endpoints.</summary>
    protected WebhooksClient Webhooks { get; private set; } = null!;

    /// <summary>Typed client for <c>/api/v1/transcripts</c> — used in batch-completion delivery tests.</summary>
    protected TranscriptsClient Transcripts { get; private set; } = null!;

    /// <summary>The public HTTPS URL to register with the Scriptube webhook API.</summary>
    protected string WebhookUrl => WebhookReceiverManager.ActiveReceiverUrl;

    /// <summary>
    /// The in-process request store for inspecting raw payloads and HMAC headers.
    /// <see langword="null"/> when an external receiver URL is configured.
    /// </summary>
    protected ReceivedRequestStore? ReceiverStore => WebhookReceiverManager.Store;

    /// <summary>
    /// <see langword="true"/> when the local <see cref="WebhookReceiver"/> is active and
    /// raw request data (body, headers) can be read back. Tests that require this should
    /// guard with <c>if (!HasLocalReceiver) Assert.Ignore("...")</c>.
    /// </summary>
    protected bool HasLocalReceiver => WebhookReceiverManager.HasLocalReceiver;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        Webhooks = new WebhooksClient(Settings);
        AllureRestLogger.Attach(Webhooks);

        Transcripts = new TranscriptsClient(Settings);
        AllureRestLogger.Attach(Transcripts);

        // Discard stale requests from a previous test so assertions are isolated.
        ReceiverStore?.Clear();
    }

    [TearDown]
    public override async Task TearDown()
    {
        AllureRestLogger.Detach(Webhooks);
        Webhooks.Dispose();

        AllureRestLogger.Detach(Transcripts);
        Transcripts.Dispose();

        await base.TearDown();
    }
}

