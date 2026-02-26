using Allure.NUnit;
using NUnit.Framework;
using Scriptube.Automation.Core.Configuration;

namespace Scriptube.Automation.Core.Tests;

/// <summary>
/// Base class for all test classes.
/// Provides access to <see cref="Settings"/> loaded from config.
/// </summary>
// [AllureNUnit] activates Allure's NUnit lifecycle hooks for every derived fixture.
[AllureNUnit]
public abstract class BaseTest
{
    protected TestSettings Settings { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        Settings = ConfigurationProvider.Get();
    }

    [TearDown]
    public virtual Task TearDown() => Task.CompletedTask;
}
