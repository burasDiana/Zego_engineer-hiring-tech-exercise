using System.Collections.Concurrent;

using Crawler.Core.Interfaces;

namespace Crawler.Core.Utils;
public class ConcurrentUrlCache : IConcurrentUrlCache
{
    private readonly ConcurrentDictionary<string, byte> _visited = new();

    public bool TryAdd(Uri uri)
    {
        var key = Canonicalize(uri);
        return _visited.TryAdd(key, 0);
    }

    private static string Canonicalize(Uri uri)
    {
        // Remove query + fragment
        var basePart = uri.GetLeftPart(UriPartial.Path);

        // Normalize trailing slash
        basePart = basePart.TrimEnd('/');

        // Lowercase for consistency
        basePart = basePart.ToLowerInvariant();

        return basePart;
    }
}
