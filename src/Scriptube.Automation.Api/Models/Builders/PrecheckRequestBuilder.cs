using Scriptube.Automation.Api.Models.Requests;

namespace Scriptube.Automation.Api.Models.Builders;

/// <summary>
/// Fluent builder for <see cref="PrecheckRequest"/>.
/// </summary>
/// <example>
/// <code>
/// var request = new PrecheckRequestBuilder()
///     .WithUrl("https://www.youtube.com/watch?v=tstENMAN001")
///     .WithUrl("https://www.youtube.com/watch?v=tstKOONL001")
///     .Build();
/// </code>
/// </example>
public sealed class PrecheckRequestBuilder
{
    private readonly List<string> _urls = [];

    /// <summary>Adds a single YouTube video URL to precheck.</summary>
    public PrecheckRequestBuilder WithUrl(string url)
    {
        _urls.Add(url);
        return this;
    }

    /// <summary>Adds multiple YouTube video URLs to precheck.</summary>
    public PrecheckRequestBuilder WithUrls(IEnumerable<string> urls)
    {
        _urls.AddRange(urls);
        return this;
    }

    /// <summary>Builds and returns the <see cref="PrecheckRequest"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no URLs have been added.</exception>
    public PrecheckRequest Build()
    {
        if (_urls.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one URL must be provided before calling Build().");
        }

        return new PrecheckRequest
        {
            Urls = [.. _urls],
        };
    }
}
