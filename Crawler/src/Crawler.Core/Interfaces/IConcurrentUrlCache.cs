namespace Crawler.Core.Interfaces;

public interface IConcurrentUrlCache
{
    bool TryAdd(Uri uri);
}
