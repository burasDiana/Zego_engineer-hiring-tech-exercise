using Crawler.Core.Interfaces;

namespace Crawler.Core.Services;

public class HttpClientWrapper : IHttpClient
{
    private readonly HttpClient client = new();

    public TimeSpan Timeout
    {
        get => client.Timeout;
        set => client.Timeout = value;
    }

    public async Task<string?> GetWithRetryAsync(Uri uri, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                var response = await client.GetAsync(uri,cancellationToken);

                // retry only on 5xx
                if ((int)response.StatusCode >= 500 &&
                    (int)response.StatusCode <= 599 &&
                    attempt < maxRetries)
                {
                    attempt++;
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
                    continue;
                }

                // fail fast on non-success
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch when (attempt < maxRetries)
            {
                // retry network-level transient errors
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
            }
            catch
            {
                return null;
            }
        }

    }
}
