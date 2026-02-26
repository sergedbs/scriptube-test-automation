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

    /// <summary>Whether the browser should launch in headless mode. Defaults to <c>true</c>.</summary>
    public bool BrowserHeadless { get; init; } = true;

    /// <summary>Slow-motion delay in milliseconds applied to every Playwright action. Set to 0 to disable. Useful for visual debugging.</summary>
    public int BrowserSlowMo { get; init; } = 0;

    /// <summary>
    /// External webhook receiver URL. When non-empty, the in-process receiver and ngrok tunnel
    /// are skipped entirely and this URL is used for webhook registration.
    /// </summary>
    public string? WebhookReceiverUrl { get; init; }

    /// <summary>Local port for the in-process webhook receiver. Used when <see cref="WebhookReceiverUrl"/> is empty.</summary>
    public int WebhookReceiverPort { get; init; } = 5099;

    /// <summary>Local port of the ngrok agent API. Used to discover the active tunnel URL.</summary>
    public int NgrokApiPort { get; init; } = 4040;

    /// <summary>Browser viewport width in pixels used for UI tests.</summary>
    public int ViewportWidth { get; init; } = 1280;

    /// <summary>Browser viewport height in pixels used for UI tests.</summary>
    public int ViewportHeight { get; init; } = 900;
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

    /// <summary>Maximum seconds to poll GetLogs waiting for a delivery entry to appear after a webhook trigger.</summary>
    public int WebhookDispatchWaitSeconds { get; init; } = 10;

    /// <summary>Maximum seconds to wait for a raw delivery to arrive in the local HttpListener receiver.</summary>
    public int WebhookDeliveryTimeoutSeconds { get; init; } = 30;

    /// <summary>Timeout in seconds for the ngrok local API HTTP request.</summary>
    public int NgrokApiTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Seconds to wait after cancelling a batch before re-reading the credit balance,
    /// allowing the server to settle the cancellation.
    /// </summary>
    public int CancelSettleSeconds { get; init; } = 2;

    /// <summary>
    /// Milliseconds to wait between authentication-related UI tests to avoid
    /// triggering the server's login rate limiter (nginx 503).
    /// </summary>
    public int AuthCooldownMs { get; init; } = 3_000;
}

public record RetrySettings
{
    /// <summary>Number of automatic retries for transient failures.</summary>
    public int Count { get; init; } = 3;

    /// <summary>Delay between retries in seconds.</summary>
    public int DelaySeconds { get; init; } = 2;
}
