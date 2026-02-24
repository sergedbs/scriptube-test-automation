using NUnit.Framework;
using Scriptube.Automation.Core.Http;
using Scriptube.Automation.Core.Reporting;
using Scriptube.Automation.Core.Tests;

namespace Scriptube.Automation.Webhooks.Tests;

/// <summary>
/// Base class for webhook test fixtures.
/// Provides the same authenticated <see cref="ApiClientBase"/> as <c>BaseApiTest</c>,
/// plus a placeholder for future webhook-specific helpers (e.g. HMAC verifier, receiver client).
/// </summary>
public abstract class BaseWebhookTest : BaseTest
{
    protected ApiClientBase ApiClient { get; private set; } = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        ApiClient = new ApiClientBase(Settings);
        AllureRestLogger.Attach(ApiClient);
    }

    [TearDown]
    public override void TearDown()
    {
        AllureRestLogger.Detach(ApiClient);
        ApiClient.Dispose();
        base.TearDown();
    }
}
