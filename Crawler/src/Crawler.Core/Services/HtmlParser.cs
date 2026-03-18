using AngleSharp;

using Crawler.Core.Interfaces;

namespace Crawler.Core.Services;

public class HtmlParser : IHtmlParser
{
    private readonly IBrowsingContext context;

    public HtmlParser()
    {
        var config = Configuration.Default;
        context = BrowsingContext.New(config);
    }

    public IEnumerable<string> ExtractUrls(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Enumerable.Empty<string>();
        }

        var document = context.OpenAsync(req => req.Content(html))
            .GetAwaiter()
            .GetResult();

        return document.QuerySelectorAll("a[href]")
            .Select(a => a.GetAttribute("href"))
            .Where(href => !string.IsNullOrWhiteSpace(href))!;
    }
}
