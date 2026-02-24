namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Export format identifiers accepted by <c>GET /api/v1/transcripts/{batch_id}/export</c>.
/// </summary>
public static class ExportFormats
{
    /// <summary>JSON array of transcript objects.</summary>
    public const string Json = "json";

    /// <summary>Plain text — transcripts concatenated.</summary>
    public const string Txt = "txt";

    /// <summary>SubRip subtitle format with timestamps.</summary>
    public const string Srt = "srt";
}
