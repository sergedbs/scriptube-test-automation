using NUnit.Framework;
using Scriptube.Automation.Core.Http;
using Scriptube.Automation.Core.Reporting;
using Scriptube.Automation.Core.Tests;

namespace Scriptube.Automation.Api.Tests;

/// <summary>
/// Base class for all API test fixtures.
/// Creates an <see cref="ApiClientBase"/> with auth injected and Allure logging wired up.
/// <para>
/// Use <see cref="CreateClient{T}"/> instead of <c>new T(Settings)</c> to obtain additional
/// typed clients — it automatically attaches Allure request/response logging and tracks the
/// client for disposal at the end of the test, so the subclass never needs to call
/// <c>AllureRestLogger.Attach</c> or <c>client.Dispose()</c> manually.
/// </para>
/// </summary>
public abstract class BaseApiTest : BaseTest
{
    protected ApiClientBase ApiClient { get; private set; } = null!;

    private readonly List<ApiClientBase> _managedClients = [];

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        ApiClient = new ApiClientBase(Settings);
        AllureRestLogger.Attach(ApiClient);
    }

    /// <summary>
    /// Creates a typed API client, attaches Allure request/response logging, and registers
    /// it for automatic disposal at the end of the test.  The subclass must not call
    /// <c>Dispose()</c> or <c>AllureRestLogger.Detach()</c> on the returned instance.
    /// </summary>
    protected T CreateClient<T>() where T : ApiClientBase
    {
        var client = (T)Activator.CreateInstance(typeof(T), Settings)!;
        AllureRestLogger.Attach(client);
        _managedClients.Add(client);
        return client;
    }

    [TearDown]
    public override async Task TearDown()
    {
        foreach (var client in _managedClients)
        {
            AllureRestLogger.Detach(client);
            client.Dispose();
        }

        _managedClients.Clear();

        AllureRestLogger.Detach(ApiClient);
        ApiClient.Dispose();
        await base.TearDown();
    }
}
