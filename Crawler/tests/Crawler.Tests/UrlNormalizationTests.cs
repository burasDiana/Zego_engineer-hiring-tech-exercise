using Crawler.Core.Services;
using FluentAssertions;
namespace Crawler.Tests;

public class UrlNormalizerTests
{
    private readonly UrlNormalizer _normalizer = new();
    private readonly Uri _baseUri = new("https://test.com");

    [Fact]
    public void Converts_Relative_To_Absolute()
    {
        var href = "/about";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().NotBeNull();
        result!.AbsoluteUri.Should().Be("https://test.com/about");
    }

    [Fact]
    public void Removes_Fragments()
    {
        var href = "https://test.com/page#section";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().NotBeNull();
        result!.AbsoluteUri.Should().Be("https://test.com/page");
    }

    [Fact]
    public void Strips_Query_Parameters()
    {
        var href = "/search?q=test&x=1";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().NotBeNull();
        result!.AbsoluteUri.Should().Be("https://test.com/search");
    }

    [Fact]
    public void Rejects_External_Domain()
    {
        var href = "https://facebook.com/page";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().BeNull();
    }

    [Fact]
    public void Canonicalizes_Path()
    {
        var href = "/a/b/../c";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().NotBeNull();
        result!.AbsoluteUri.Should().Be("https://test.com/a/c");
    }

    [Fact]
    public void Rejects_Invalid_Urls()
    {
        var href = "::::://test-malicious.com";

        var result = _normalizer.Normalize(_baseUri, href);

        result.Should().BeNull();
    }
}
