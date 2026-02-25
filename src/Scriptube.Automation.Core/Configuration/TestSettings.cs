namespace Scriptube.Automation.Core.Configuration;

/// <summary>Strongly-typed settings loaded from appsettings.json + environment variables.</summary>
public record TestSettings
{
    public string BaseUrl { get; init; } = "https://scriptube.me";
    public string ApiKey { get; init; } = string.Empty;
    public CredentialsSettings Credentials { get; init; } = new();
    public TimeoutSettings Timeouts { get; init; } = new();
    public RetrySettings Retry { get; init; } = new();
    public string Browser { get; init; } = "chromium";
    /// <summary>
    /// External webhook receiver URL. When non-empty, the in-process receiver and ngrok tunnel
    /// are skipped entirely and this URL is used for webhook registration.
    /// </summary>
    public string? WebhookReceiverUrl { get; init; }

    /// <summary>Local port for the in-process webhook receiver. Used when <see cref="WebhookReceiverUrl"/> is empty.</summary>
    public int WebhookReceiverPort { get; init; } = 5099;
}

public record CredentialsSettings
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record TimeoutSettings
{
    /// <summary>General HTTP request timeout in seconds.</summary>
    public int RequestSeconds { get; init; } = 30;

    /// <summary>How long to wait between batch status poll calls in seconds.</summary>
    public int PollIntervalSeconds { get; init; } = 3;

    /// <summary>Maximum time to wait for a batch to complete in seconds.</summary>
    public int PollTimeoutSeconds { get; init; } = 120;

    /// <summary>Playwright navigation timeout in milliseconds.</summary>
    public int PlaywrightNavigationMs { get; init; } = 30_000;

    /// <summary>Playwright action timeout in milliseconds.</summary>
    public int PlaywrightActionMs { get; init; } = 10_000;
}

public record RetrySettings
{
    /// <summary>Number of automatic retries for transient failures.</summary>
    public int Count { get; init; } = 3;

    /// <summary>Delay between retries in seconds.</summary>
    public int DelaySeconds { get; init; } = 2;
}
