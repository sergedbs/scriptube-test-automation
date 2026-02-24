namespace Scriptube.Automation.Api.TestData;

/// <summary>
/// Deterministic test video IDs (all prefixed with <c>tst</c>).
/// No real YouTube API calls are made for these IDs.
/// </summary>
public static class VideoIds
{
    // ── Success videos ────────────────────────────────────────────────────────

    /// <summary>English manual captions — cheapest path (1 credit).</summary>
    public const string EnglishManual = "tstENMAN001";

    /// <summary>English auto-generated captions (1 credit).</summary>
    public const string EnglishAuto = "tstENAUT001";

    /// <summary>Korean only — auto-translated to English.</summary>
    public const string KoreanOnly = "tstKOONL001";

    /// <summary>Spanish only — auto-translated to English.</summary>
    public const string SpanishOnly = "tstESAUT001";

    /// <summary>Multi-language video — English track selected (1 credit).</summary>
    public const string MultiLanguage = "tstMULTI001";

    /// <summary>French — YouTube auto-translate to English (free).</summary>
    public const string FrenchAutoTranslate = "tstYTTRN001";

    /// <summary>No captions — ElevenLabs AI fallback (paid plan required).</summary>
    public const string NoCaptions = "tstNOCAP001";

    /// <summary>Forced ElevenLabs transcription (paid plan required).</summary>
    public const string ElevenLabsForced = "tstELABS001";

    /// <summary>ElevenLabs + translation German → English.</summary>
    public const string ElevenLabsTranslation = "tstELTRN001";

    /// <summary>Cached YouTube transcript — cache hit path.</summary>
    public const string CachedYouTube = "tstCACHE001";

    /// <summary>Cached ElevenLabs transcript.</summary>
    public const string CachedElevenLabs = "tstCACEL001";

    // ── Error videos ─────────────────────────────────────────────────────────

    /// <summary>Private video — returns item-level error.</summary>
    public const string Private = "tstPRIVT001";

    /// <summary>Deleted video — returns item-level error.</summary>
    public const string Deleted = "tstDELET001";

    /// <summary>Age-restricted video — returns item-level error.</summary>
    public const string AgeRestricted = "tstAGERS001";

    /// <summary>120-minute video — too long, returns error.</summary>
    public const string TooLong = "tstLONG0001";

    /// <summary>Rate-limited — triggers retry/recovery flow.</summary>
    public const string RateLimited = "tstRLIMT001";

    /// <summary>Connection timeout — returns item-level error.</summary>
    public const string Timeout = "tstTIMEO001";

    /// <summary>Malformed data — returns processing error.</summary>
    public const string Invalid = "tstINVLD001";

    // ── Full YouTube URLs ─────────────────────────────────────────────────────

    private const string YouTubeBase = "https://www.youtube.com/watch?v=";

    public static string ToUrl(string videoId) => $"{YouTubeBase}{videoId}";

    public static readonly string EnglishManualUrl = ToUrl(EnglishManual);
    public static readonly string EnglishAutoUrl = ToUrl(EnglishAuto);
    public static readonly string KoreanOnlyUrl = ToUrl(KoreanOnly);
    public static readonly string SpanishOnlyUrl = ToUrl(SpanishOnly);
    public static readonly string MultiLanguageUrl = ToUrl(MultiLanguage);
    public static readonly string FrenchAutoTranslateUrl = ToUrl(FrenchAutoTranslate);
    public static readonly string NoCaptionsUrl = ToUrl(NoCaptions);
    public static readonly string ElevenLabsForcedUrl = ToUrl(ElevenLabsForced);
    public static readonly string ElevenLabsTranslationUrl = ToUrl(ElevenLabsTranslation);
    public static readonly string CachedYouTubeUrl = ToUrl(CachedYouTube);
    public static readonly string CachedElevenLabsUrl = ToUrl(CachedElevenLabs);
    public static readonly string PrivateUrl = ToUrl(Private);
    public static readonly string DeletedUrl = ToUrl(Deleted);
    public static readonly string AgeRestrictedUrl = ToUrl(AgeRestricted);
    public static readonly string TooLongUrl = ToUrl(TooLong);
    public static readonly string RateLimitedUrl = ToUrl(RateLimited);
    public static readonly string TimeoutUrl = ToUrl(Timeout);
    public static readonly string InvalidUrl = ToUrl(Invalid);
}
