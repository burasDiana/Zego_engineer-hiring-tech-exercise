using Crawler;
using Crawler.Core.Interfaces;
using Crawler.Core.Services;
using Crawler.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

CliOptions opt;

try
{
    opt = CliOptionsParser.ParseInput(args);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return;
}

services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(opt.Debug ? LogLevel.Debug : LogLevel.Information);
});

// service DI
services.AddSingleton<IHttpClient, HttpClientWrapper>();
services.AddSingleton<IHtmlParser, HtmlParser>();
services.AddSingleton<IUrlNormalizer, UrlNormalizer>();
services.AddSingleton<IRobotsClient, RobotsClient>();
services.AddSingleton<IConcurrentUrlCache, ConcurrentUrlCache>();
services.AddSingleton<WebCrawler>();

var provider = services.BuildServiceProvider();
var crawler = provider.GetRequiredService<WebCrawler>();

await crawler.CrawlAsync(opt.BaseUri, opt.MaxConcurrency, opt.RespectRobots,
    result =>
    {
        foreach (var link in result.DiscoveredUrls)
        {
            Console.WriteLine($"{link}");
        }
        return Task.CompletedTask;
    }
    );

