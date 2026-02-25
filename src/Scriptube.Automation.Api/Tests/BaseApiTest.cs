using NUnit.Framework;
using Scriptube.Automation.Core.Http;
using Scriptube.Automation.Core.Reporting;
using Scriptube.Automation.Core.Tests;

namespace Scriptube.Automation.Api.Tests;

/// <summary>
/// Base class for all API test fixtures.
/// Creates an <see cref="ApiClientBase"/> with auth injected and Allure logging wired up.
/// </summary>
public abstract class BaseApiTest : BaseTest
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
    public override async Task TearDown()
    {
        AllureRestLogger.Detach(ApiClient);
        ApiClient.Dispose();
        await base.TearDown();
    }
}
