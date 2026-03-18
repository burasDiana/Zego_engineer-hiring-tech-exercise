using Crawler.Core.Interfaces;

namespace Crawler.Core.Services;

public class UrlNormalizer : IUrlNormalizer
{
    public Uri? Normalize(Uri baseUri, string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        // Reject malformed absolute URLs like "::::://malicious.com"
        if (href.Contains("://", StringComparison.Ordinal) &&
            !href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Reject non-URL schemes
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Try to build an absolute URI
        if (!Uri.TryCreate(baseUri, href, out var absolute))
        {
            return null;
        }

        // Only allow HTTP/HTTPS
        if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        // Enforce same-domain crawling
        if (!IsSameDomain(baseUri, absolute))
        {
            return null;
        }

        // Remove fragment (#section) and query (?x=1)
        var builder = new UriBuilder(absolute)
        {
            Fragment = string.Empty,
            Query = string.Empty
        };

        return builder.Uri;
    }

    public bool IsSameDomain(Uri baseUri, Uri candidate)
        => string.Equals(baseUri.Host, candidate.Host, StringComparison.OrdinalIgnoreCase);
}
