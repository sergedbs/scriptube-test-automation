namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Constants used in validation and negative-path smoke tests.
/// These values exercise server-side rejection logic and are intentionally invalid inputs.
/// </summary>
public static class ValidationTestData
{
    /// <summary>
    /// A valid URL that is not a YouTube video — used to verify that the API rejects
    /// non-YouTube URLs with a 4xx response.
    /// </summary>
    public const string NonYouTubeUrl = "https://vimeo.com/123456789";
}
