namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Constants used across webhook smoke and regression tests.
/// </summary>
public static class WebhookTestData
{
    // Signing secrets

    /// <summary>Signing secret used in webhook smoke tests (min 16 chars).</summary>
    public const string SmokeSecret = "smoke-test-secret-key-1234";

    /// <summary>Signing secret used in webhook regression/lifecycle tests.</summary>
    public const string RegressionSecret = "regression-test-secret-key-5678";

    /// <summary>Signing secret used exclusively for the HMAC verification test.</summary>
    public const string HmacVerificationSecret = "hmac-verification-secret-abc-xyz";

    // Event names

    /// <summary>Event fired when a transcript batch finishes processing.</summary>
    public const string EventBatchCompleted = "batch.completed";

    /// <summary>Event fired when a single transcript becomes available.</summary>
    public const string EventTranscriptReady = "transcript.ready";

    /// <summary>Event fired when the credit balance falls below the threshold.</summary>
    public const string EventCreditsLow = "credits.low";

    // SSRF probe URLs

    /// <summary>Localhost probe — must be rejected by the API (SSRF protection).</summary>
    public const string SsrfLocalhost = "http://localhost/x";

    /// <summary>192.168.x.x private-range probe — must be rejected by the API.</summary>
    public const string SsrfPrivate192 = "http://192.168.1.1/x";

    /// <summary>10.x.x.x private-range probe — must be rejected by the API.</summary>
    public const string SsrfPrivate10 = "http://10.0.0.1/x";
}
