namespace Crawler.Core.Interfaces;

public interface IHtmlParser
{
    IEnumerable<string> ExtractUrls(string html);
}
