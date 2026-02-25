using System.Text.Json.Serialization;

namespace Scriptube.Automation.Api.Models.Requests;

/// <summary>Request body for <c>POST /api/v1/transcripts</c>.</summary>
public sealed class TranscriptRequest
{
    /// <summary>List of YouTube video or playlist URLs to process.</summary>
    [JsonPropertyName("urls")]
    public List<string> Urls { get; init; } = [];

    /// <summary>When <c>true</c>, uses bring-your-own-key path (different credit cost).</summary>
    [JsonPropertyName("use_byok")]
    public bool UseByok { get; init; } = false;

    /// <summary>When <c>true</c>, translates non-English transcripts to English.</summary>
    [JsonPropertyName("translate_to_english")]
    public bool TranslateToEnglish { get; init; } = false;
}
