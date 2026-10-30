namespace PowerPointCompareForGit.Tests;

public class PageRangeTests
{
    [Fact]
    public void SinglePage()
    {
        Assert.Equal(new PageRange(40, 40), PageRange.Parse("40"));
    }

    [Fact]
    public void Range()
    {
        Assert.Equal(new PageRange(2, 25), PageRange.Parse("2-25"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("3-")]
    [InlineData("5-2")]
    [InlineData("1-2-3")]
    [InlineData("abc")]
    [InlineData("99999999999")]
    public void RejectsInvalidText(string text)
    {
        Assert.Null(PageRange.Parse(text));
    }

    [Fact]
    public void PagesListsEveryPageInTheRange()
    {
        Assert.Equal([2, 3, 4], new PageRange(2, 4).Pages);
    }

    [Fact]
    public void ClampLeavesPagesInsideTheDeckAlone()
    {
        Assert.Equal(new PageRange(2, 3), new PageRange(2, 3).ClampTo(3));
    }

    [Fact]
    public void ClampMovesEndOfRangeToLastSlide()
    {
        Assert.Equal(new PageRange(2, 3), new PageRange(2, 25).ClampTo(3));
    }

    [Fact]
    public void ClampMovesPagePastTheEndToLastSlide()
    {
        Assert.Equal(new PageRange(3, 3), new PageRange(40, 40).ClampTo(3));
    }

    [Fact]
    public void ClampMovesWholeRangePastTheEndToLastSlide()
    {
        Assert.Equal(new PageRange(3, 3), new PageRange(40, 50).ClampTo(3));
    }
}
