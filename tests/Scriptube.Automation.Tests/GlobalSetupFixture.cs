using Allure.Net.Commons;
using NUnit.Framework;
using Scriptube.Automation.Core.Configuration;
using Serilog;

namespace Scriptube.Automation.Tests;

/// <summary>
/// Assembly-level setup for the test suite.
/// Initializes Serilog and writes Allure environment.properties so reports
/// include runtime metadata on the Overview tab.
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

        var settings = ConfigurationProvider.Get();

        Log.Information("Global test setup complete. BaseUrl={BaseUrl}", settings.BaseUrl);

        WriteAllureEnvironmentProperties(settings);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Log.CloseAndFlush();
    }

    private static void WriteAllureEnvironmentProperties(TestSettings settings)
    {
        try
        {
            var resultsDir = AllureLifecycle.Instance.ResultsDirectory;
            Directory.CreateDirectory(resultsDir);

            var filePath = Path.Combine(resultsDir, "environment.properties");

            var lines = new[]
            {
                $"Base.URL={settings.BaseUrl}",
                $"Runtime=.NET {Environment.Version}",
                $"Framework=NUnit",
                $"HTTP.Client=RestSharp",
                $"UI.Automation=Playwright",
                $"OS={Environment.OSVersion}",
                $"Machine={Environment.MachineName}",
                $"Date={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            };

            File.WriteAllLines(filePath, lines);
        }
        catch (Exception ex)
        {
            // Never fail the test run because of reporting setup.
            Log.Warning(ex, "Failed to write Allure environment.properties — report Overview tab may be missing environment data");
        }
    }
}
