namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Deterministic test playlist URLs (all prefixed with <c>PLtst</c>).
/// No real YouTube API calls are made for these playlists.
/// </summary>
public static class PlaylistUrls
{
    private const string YouTubePlaylistBase = "https://www.youtube.com/playlist?list=";

    /// <summary>3 success videos — all items complete successfully.</summary>
    public const string AllSuccess = $"{YouTubePlaylistBase}PLtstOK00001";

    /// <summary>Mixed: English + Korean + ElevenLabs videos.</summary>
    public const string Mixed = $"{YouTubePlaylistBase}PLtstMIX0001";

    /// <summary>5 videos — mix of successes and errors.</summary>
    public const string AllMixed = $"{YouTubePlaylistBase}PLtstALL0001";

    /// <summary>3 error-only videos — all items fail.</summary>
    public const string AllErrors = $"{YouTubePlaylistBase}PLtstERR0001";
}
