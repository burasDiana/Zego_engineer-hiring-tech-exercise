namespace Crawler;

public static class CliOptionsParser
{
    public static CliOptions ParseInput(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Base URL is required. Example: crawler https://example.com");
        }

        // required
        var baseUri = ParseUri(args[0]);

        // optional
        var maxConcurrency = 8;
        var respectRobots = true;
        var debug = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--no-robots":
                case "--nr":
                    respectRobots = false;
                    break;

                case "--debug":
                case "--d":
                    debug = true;
                    break;

                case "--concurrency":
                case "--c":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --concurrency");
                    }

                    if (!int.TryParse(args[++i], out maxConcurrency) || maxConcurrency <= 0)
                    {
                        throw new ArgumentException("Invalid value for --concurrency");
                    }

                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return new CliOptions(
            BaseUri: baseUri,
            MaxConcurrency: maxConcurrency,
            RespectRobots: respectRobots,
            Debug: debug
        );
    }

    private static Uri ParseUri(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL: {input}");
        }

        return uri;
    }
}
