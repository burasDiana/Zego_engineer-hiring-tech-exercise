using Crawler.Core.Interfaces;
using Crawler.Core.Models;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Services;

public class RobotsClient : IRobotsClient
{
    private readonly IHttpClient http;
    private readonly ILogger<RobotsClient> logger;

    // cache robots.txt rules per domain
    private readonly Dictionary<string, RobotsRules> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim @lock = new(1, 1);

    public RobotsClient(IHttpClient http, ILogger<RobotsClient> logger)
    {
        this.http = http;
        this.logger = logger;
    }

    public async Task<bool> IsAllowedAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var host = uri.Host;

        RobotsRules rules;

        // Ensure robots.txt is loaded once per domain
        await @lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!cache.TryGetValue(host, out rules!))
            {
                rules = await FetchRobotsRulesAsync(uri, cancellationToken).ConfigureAwait(false);
                cache[host] = rules;
                logger.LogDebug($"Loaded robots.txt for {host}: {rules.Disallowed.Count} disallowed paths");
            }
        }
        finally
        {
            @lock.Release();
        }

        // Check if any disallowed path matches the URL path
        foreach (var disallowed in rules.Disallowed)
        {
            if (uri.AbsolutePath.StartsWith(disallowed, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<RobotsRules> FetchRobotsRulesAsync(Uri uri, CancellationToken cancellationToken)
    {
        var robotsUri = new Uri($"{uri.Scheme}://{uri.Host}/robots.txt");

        logger.LogDebug("Fetching robots.txt from {Url}", robotsUri);

        var content = await http.GetWithRetryAsync(robotsUri,2, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
        {
            return new RobotsRules(); // allow all
        }

        return ParseRobotsTxt(content);
    }

    private static RobotsRules ParseRobotsTxt(string content)
    {
        var rules = new RobotsRules();

        using var reader = new StringReader(content);
        string? line;

        bool appliesToAll = false;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.StartsWith("#"))
            {
                continue;
            }

            // only look at rules that apply to all crawlers
            if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                var agent = line.Substring("User-agent:".Length).Trim();
                appliesToAll = agent == "*" || agent == string.Empty;
            }

            if (!appliesToAll)
            {
                continue;
            }

            if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var path = line.Substring("Disallow:".Length).Trim();

                if (!string.IsNullOrWhiteSpace(path))
                {
                    rules.Disallowed.Add(path);
                }
            }
        }

        return rules;
    }
}
