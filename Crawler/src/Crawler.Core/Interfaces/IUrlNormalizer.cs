namespace Crawler.Core.Interfaces;

public interface IUrlNormalizer
{
    Uri? Normalize(Uri baseUri, string href);
    bool IsSameDomain(Uri baseUri, Uri candidate);
}
