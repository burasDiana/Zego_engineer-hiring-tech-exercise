using Crawler.Core.Services;
using Crawler.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Crawler.Tests;

public class WebCrawlerTests
{
    private readonly Mock<IHttpClient> _http = new();
    private readonly Mock<IHtmlParser> _parser = new();
    private readonly Mock<IUrlNormalizer> _normalizer = new();
    private readonly Mock<IRobotsClient> _robots = new();
    private readonly Mock<IConcurrentUrlCache> _visited = new();
    private readonly Mock<ILogger<WebCrawler>> _logger = new();

    private WebCrawler CreateCrawler() =>
        new WebCrawler(_http.Object, _parser.Object, _normalizer.Object, _robots.Object, _visited.Object, _logger.Object);

    private static readonly Uri Start = new("https://test.com");
    private static readonly Uri Page1 = new("https://test.com/page1");
    private static readonly Uri Page2 = new("https://test.com/page2");

    [Fact]
    public async Task CrawlAsync_Can_Process_Start_Url()
    {
        _visited.Setup(v => v.TryAdd(Start)).Returns(true);
        _robots.Setup(r => r.IsAllowedAsync(Start, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _http.Setup(h => h.GetWithRetryAsync(Start, It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("<a href=\"/page1\"></a>");

        _parser.Setup(p => p.ExtractUrls(It.IsAny<string>()))
               .Returns(new[] { "/page1" });

        _normalizer.Setup(n => n.Normalize(Start, "/page1"))
                   .Returns(Page1);

        _visited.Setup(v => v.TryAdd(Page1)).Returns(true);

        var crawler = CreateCrawler();
        await crawler.CrawlAsync(Start);

        _http.Verify(h => h.GetWithRetryAsync(Start, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _parser.Verify(p => p.ExtractUrls(It.IsAny<string>()), Times.Once);
        _normalizer.Verify(n => n.Normalize(Start, "/page1"), Times.Once);
    }

    [Fact]
    public async Task CrawlAsync_Adheres_to_Robots_Rules()
    {
        _visited.Setup(v => v.TryAdd(Start)).Returns(true);
        _robots.Setup(r => r.IsAllowedAsync(Start, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var crawler = CreateCrawler();
        await crawler.CrawlAsync(Start);

        _http.Verify(h => h.GetWithRetryAsync(It.IsAny<Uri>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _parser.Verify(p => p.ExtractUrls(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CrawlAsync_Does_Not_Revisit_Urls()
    {
        _visited.Setup(v => v.TryAdd(Start)).Returns(true);
        _visited.Setup(v => v.TryAdd(Page1)).Returns(false); // already visited

        _robots.Setup(r => r.IsAllowedAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _http.Setup(h => h.GetWithRetryAsync(Start, It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("<a href=\"/page1\"></a>");

        _parser.Setup(p => p.ExtractUrls(It.IsAny<string>()))
               .Returns(new[] { "/page1" });

        _normalizer.Setup(n => n.Normalize(Start, "/page1"))
                   .Returns(Page1);

        var crawler = CreateCrawler();
        await crawler.CrawlAsync(Start);

        _http.Verify(h => h.GetWithRetryAsync(Page1, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrawlAsync_Skips_Urls_That_Fail_Normalization()
    {
        _visited.Setup(v => v.TryAdd(Start)).Returns(true);
        _robots.Setup(r => r.IsAllowedAsync(Start, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _http.Setup(h => h.GetWithRetryAsync(Start, It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("<a href=\"/bad\"></a>");

        _parser.Setup(p => p.ExtractUrls(It.IsAny<string>()))
               .Returns(new[] { "/bad" });

        _normalizer.Setup(n => n.Normalize(Start, "/bad"))
                   .Returns((Uri?)null);

        var crawler = CreateCrawler();
        await crawler.CrawlAsync(Start);

        _visited.Verify(v => v.TryAdd(It.Is<Uri>(u => u.AbsoluteUri.Contains("bad"))), Times.Never);
    }

    [Fact]
    public async Task CrawlAsync_Stops_When_Cancelled()
    {
        _visited.Setup(v => v.TryAdd(It.IsAny<Uri>())).Returns(true);
        _robots.Setup(r => r.IsAllowedAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _http.Setup(h => h.GetWithRetryAsync(It.IsAny<Uri>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .Returns(async () =>
             {
                 await Task.Delay(5000);
                 return "<html></html>";
             });

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var crawler = CreateCrawler();

        await FluentActions.Invoking(() => crawler.CrawlAsync(Start, 8, true, res  => Task.CompletedTask, cts.Token))
                           .Should()
                           .ThrowAsync<OperationCanceledException>();
    }
}
