using NUnit.Framework;
using Scriptube.Automation.Core.Configuration;
using Serilog;

namespace Scriptube.Automation.Core.Tests;

/// <summary>
/// Root base class for all test fixtures.
/// Loads configuration and sets up Serilog once per assembly run.
/// </summary>
[SetUpFixture]
public class GlobalSetupFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Debug()
            .CreateLogger();

        Log.Information("Global test setup complete. BaseUrl={BaseUrl}",
            ConfigurationProvider.Get().BaseUrl);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Log.CloseAndFlush();
    }
}
