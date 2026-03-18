namespace Crawler.Core.Models;

public class RobotsRules
{
    public HashSet<string> Disallowed { get; } = new();
}
