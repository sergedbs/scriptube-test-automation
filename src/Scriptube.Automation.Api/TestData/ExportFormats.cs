namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Export format identifiers accepted by <c>GET /api/v1/transcripts/{batch_id}/export</c>.
/// Allowed values per OpenAPI spec: <c>^(json|csv|txt)$</c>.
/// </summary>
public static class ExportFormats
{
    /// <summary>JSON array of transcript objects.</summary>
    public const string Json = "json";

    /// <summary>Comma-separated values.</summary>
    public const string Csv = "csv";

    /// <summary>Plain text — transcripts concatenated.</summary>
    public const string Txt = "txt";
}
