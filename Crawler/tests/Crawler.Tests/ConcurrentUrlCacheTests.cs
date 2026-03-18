using Crawler.Core.Utils;
using FluentAssertions;

namespace Crawler.Tests;

public class VisitedUrlCacheTests
{
    [Fact]
    public void TryAdd_FirstTime_ReturnsTrue()
    {
        var store = new ConcurrentUrlCache();
        var uri = new Uri("https://test.com/");

        store.TryAdd(uri).Should().BeTrue();
    }

    [Fact]
    public void TryAdd_SecondTime_ReturnsFalse()
    {
        var store = new ConcurrentUrlCache();
        var uri = new Uri("https://test.com/");

        store.TryAdd(uri);
        store.TryAdd(uri).Should().BeFalse();
    }

    [Fact]
    public void TryAdd_TrailingSlash_Normalized()
    {
        var store = new ConcurrentUrlCache();

        store.TryAdd(new Uri("https://test.com/")).Should().BeTrue();
        store.TryAdd(new Uri("https://test.com")).Should().BeFalse();
    }

    [Fact]
    public void TryAdd_QueryAndFragment_AreIgnored()
    {
        var store = new ConcurrentUrlCache();

        store.TryAdd(new Uri("https://test.com/page?x=1#top")).Should().BeTrue();
        store.TryAdd(new Uri("https://test.com/page")).Should().BeFalse();
    }

    [Fact]
    public void TryAdd_IsCaseInsensitive()
    {
        var store = new ConcurrentUrlCache();

        store.TryAdd(new Uri("https://TEST.com/Page")).Should().BeTrue();
        store.TryAdd(new Uri("https://test.com/page")).Should().BeFalse();
    }
}

