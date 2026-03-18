namespace Crawler;

public record CliOptions(
    Uri BaseUri,
    int MaxConcurrency,
    bool RespectRobots,
    bool Debug
);
