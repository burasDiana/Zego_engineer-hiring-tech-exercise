namespace Crawler.Core.Interfaces;

public interface IRobotsClient
{
    Task<bool> IsAllowedAsync(Uri uri, CancellationToken cancellationToken = default);
}
