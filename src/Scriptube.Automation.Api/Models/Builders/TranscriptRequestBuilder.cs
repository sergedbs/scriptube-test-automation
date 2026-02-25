using Scriptube.Automation.Api.Models.Requests;

namespace Scriptube.Automation.Api.Models.Builders;

/// <summary>
/// Fluent builder for <see cref="TranscriptRequest"/>.
/// </summary>
/// <example>
/// <code>
/// var request = new TranscriptRequestBuilder()
///     .WithUrl("https://www.youtube.com/watch?v=tstENMAN001")
///     .WithTranslation()
///     .Build();
/// </code>
/// </example>
public sealed class TranscriptRequestBuilder
{
    private readonly List<string> _urls = [];
    private bool _translateToEnglish;
    private bool _useByok;

    /// <summary>Adds a single YouTube video URL to the batch.</summary>
    public TranscriptRequestBuilder WithUrl(string url)
    {
        _urls.Add(url);
        return this;
    }

    /// <summary>Adds multiple YouTube video URLs to the batch.</summary>
    public TranscriptRequestBuilder WithUrls(IEnumerable<string> urls)
    {
        _urls.AddRange(urls);
        return this;
    }

    /// <summary>
    /// Adds a YouTube playlist URL to the batch.
    /// Semantically identical to <see cref="WithUrl"/> — kept for readability in tests.
    /// </summary>
    public TranscriptRequestBuilder WithPlaylist(string playlistUrl)
    {
        _urls.Add(playlistUrl);
        return this;
    }

    /// <summary>Enables automatic translation of non-English transcripts to English.</summary>
    public TranscriptRequestBuilder WithTranslation()
    {
        _translateToEnglish = true;
        return this;
    }

    /// <summary>Enables the bring-your-own-key (BYOK) processing path.</summary>
    public TranscriptRequestBuilder WithByok()
    {
        _useByok = true;
        return this;
    }

    /// <summary>Builds and returns the <see cref="TranscriptRequest"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no URLs have been added.</exception>
    public TranscriptRequest Build()
    {
        if (_urls.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one URL must be provided before calling Build().");
        }

        return new TranscriptRequest
        {
            Urls = [.. _urls],
            TranslateToEnglish = _translateToEnglish,
            UseByok = _useByok,
        };
    }
}
