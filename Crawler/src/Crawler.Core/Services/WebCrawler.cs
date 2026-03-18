using System.Collections.Concurrent;
using Crawler.Core.Interfaces;
using Crawler.Core.Models;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Services;

public class WebCrawler
{
    private readonly IHttpClient httpClient;
    private readonly IHtmlParser parser;
    private readonly IUrlNormalizer normalizer;
    private readonly IRobotsClient robots;
    private readonly IConcurrentUrlCache visitedCache;
    private readonly ILogger<WebCrawler> logger;

    private int activeWorkers = 0;

    public WebCrawler(
        IHttpClient httpClient,
        IHtmlParser parser,
        IUrlNormalizer normalizer,
        IRobotsClient robots,
        IConcurrentUrlCache visitedCache,
        ILogger<WebCrawler> logger)
    {
        this.httpClient = httpClient;
        this.parser = parser;
        this.normalizer = normalizer;
        this.robots = robots;
        this.visitedCache = visitedCache;
        this.logger = logger;

        httpClient.Timeout = TimeSpan.FromSeconds(2);
    }

    public async Task CrawlAsync(Uri startUri, int maxConcurrency = 8, bool respectRobots = true, Func<CrawlResult, Task>? onResult = null, CancellationToken cancellationToken = default)
    {
        var queue = new ConcurrentQueue<Uri>();

        if (visitedCache.TryAdd(startUri))
        {
            queue.Enqueue(startUri);
        }

        var tasks = new List<Task>();

        for (var i = 0; i < maxConcurrency; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!queue.TryDequeue(out var current))
                    {
                        // exit condition -> If there are no active workers AND queue is empty → crawl is finished
                        if (Interlocked.CompareExchange(ref activeWorkers, 0, 0) == 0)
                        {
                            break;
                        }

                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    Interlocked.Increment(ref activeWorkers);
                    try
                    {
                        await ProcessUrlAsync(current, queue, respectRobots, onResult, cancellationToken);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref activeWorkers);
                    }
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ProcessUrlAsync(Uri uri, ConcurrentQueue<Uri> queue, bool respectRobots, Func<CrawlResult, Task>? onResult, CancellationToken cancellationToken)
    {
        logger.LogDebug("Crawling {Url}", uri);

        var results = new List<Uri>();

        try
        {
            // check robots rules
            if (respectRobots && !await robots.IsAllowedAsync(uri, cancellationToken))
            {
                logger.LogDebug("Blocked by robots.txt: {Url}", uri);
                return;
            }

            // fetch HTML
            var html = await httpClient.GetWithRetryAsync(uri, 5, cancellationToken);
            if (html is null)
            {
                logger.LogDebug("Failed to fetch {Url}", uri);
                return;
            }

            // extract raw hrefs
            var hrefs = parser.ExtractUrls(html);

            foreach (var href in hrefs)
            {
                var normalizedUri = normalizer.Normalize(uri, href);
                if (normalizedUri is null)
                {
                    continue;
                }

                // stay within the domain
                if (normalizedUri.Host != uri.Host)
                {
                    continue;
                }

                if (visitedCache.TryAdd(normalizedUri))
                {
                    results.Add(normalizedUri);
                    queue.Enqueue(normalizedUri);
                }
            }
        }
        catch (Exception)
        {
            logger.LogError("Error crawling current {Url}", uri);
            throw;
        }
        finally
        {
            // Only return results if we actually found URLs
            if (onResult != null && results.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                await onResult(new CrawlResult
                {
                    Url = uri,
                    DiscoveredUrls = results
                });
            }
        }
    }
}
