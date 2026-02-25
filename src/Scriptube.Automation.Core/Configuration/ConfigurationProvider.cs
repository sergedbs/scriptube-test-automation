using dotenv.net;
using Microsoft.Extensions.Configuration;

namespace Scriptube.Automation.Core.Configuration;

/// <summary>
/// Builds the unified <see cref="TestSettings"/> from the layered sources:
///   1. appsettings.json (defaults)
///   2. appsettings.{env}.json (optional override)
///   3. .env file (local developer secrets — gitignored)
///   4. Environment variables (highest priority, used in CI)
///
/// Env-var mapping (the IConfiguration key → env var):
///   ApiKey        → SCRIPTUBE_API_KEY
///   BaseUrl       → SCRIPTUBE_BASE_URL
///   Credentials:Email    → SCRIPTUBE_EMAIL
///   Credentials:Password → SCRIPTUBE_PASSWORD
///   WebhookReceiverUrl   → WEBHOOK_RECEIVER_URL
/// </summary>
public static class ConfigurationProvider
{
    private static TestSettings? _cached;
    private static readonly object _lock = new();

    public static TestSettings Get()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        lock (_lock)
        {
            if (_cached is not null)
            {
                return _cached;
            }
            _cached = Build();
        }

        return _cached;
    }

    private static TestSettings Build()
    {
        // Load .env file when running locally (no-op if file does not exist).
        DotEnv.Fluent()
            .WithoutExceptions()
            .WithProbeForEnv(probeLevelsToSearch: 8)
            .Load();

        var environment = Environment.GetEnvironmentVariable("TEST_ENV") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(FindSettingsDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var settings = new TestSettings
        {
            BaseUrl = config["SCRIPTUBE_BASE_URL"]
                      ?? config["BaseUrl"]
                      ?? "https://scriptube.me",

            ApiKey = config["SCRIPTUBE_API_KEY"]
                     ?? config["ApiKey"]
                     ?? string.Empty,

            Credentials = new CredentialsSettings
            {
                Email = config["SCRIPTUBE_EMAIL"]
                        ?? config["Credentials:Email"]
                        ?? string.Empty,
                Password = config["SCRIPTUBE_PASSWORD"]
                           ?? config["Credentials:Password"]
                           ?? string.Empty,
            },

            Timeouts = BindSection<TimeoutSettings>(config, "Timeouts") ?? new TimeoutSettings(),

            Retry = BindSection<RetrySettings>(config, "Retry") ?? new RetrySettings(),

            Browser = config["SCRIPTUBE_BROWSER"]
                      ?? config["Browser"]
                      ?? "chromium",

            WebhookReceiverUrl = config["WEBHOOK_RECEIVER_URL"]
                                 ?? config["WebhookReceiverUrl"],
        };

        return settings;
    }

    private static T? BindSection<T>(IConfiguration config, string section) where T : new()
    {
        var obj = new T();
        config.GetSection(section).Bind(obj);
        return obj;
    }

    /// <summary>
    /// Walks up from the executing assembly directory until it finds appsettings.json.
    /// Falls back to <see cref="Directory.GetCurrentDirectory"/> if not found.
    /// </summary>
    private static string FindSettingsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
