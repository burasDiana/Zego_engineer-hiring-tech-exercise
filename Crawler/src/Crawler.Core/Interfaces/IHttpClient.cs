namespace Crawler.Core.Interfaces;

public interface IHttpClient
{
    TimeSpan Timeout { get; set; }
    Task<string?> GetWithRetryAsync(Uri uri, int maxRetries, CancellationToken cancellationToken = default);
}

