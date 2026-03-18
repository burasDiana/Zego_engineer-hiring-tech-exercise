namespace Crawler.Core.Models;

public class CrawlResult
{
    // ToDo write results to output
    public required Uri Url { get; init; }
    public required IReadOnlyList<Uri> DiscoveredUrls { get; init; }
}
