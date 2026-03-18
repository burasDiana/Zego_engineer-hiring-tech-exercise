using Crawler.Core.Services;
using FluentAssertions;

namespace Crawler.Tests;

public class HtmlParserTests
{
    [Fact]
    public void Parse_ExtractsAbsoluteAndRelativeLinks()
    {
        var parser = new HtmlParser();
        var html = """
                       <a href="/a"></a>
                       <a href="https://example.com/b"></a>
                   """;

        var links = parser.ExtractUrls(html);

        links.Should().Contain("/a");
        links.Should().Contain("https://example.com/b");
    }

    [Fact]
    public void Parse_HandlesMalformedHtml()
    {
        var parser = new HtmlParser();
        var html = "<a href=\"/a\"><div><span></a>";

        var links = parser.ExtractUrls(html);

        links.Should().Contain("/a");
    }
}
