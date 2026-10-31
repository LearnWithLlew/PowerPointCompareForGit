namespace PowerPointCompareForGit.Tests;

public class SlideAlignmentTests
{
    private static SlideAlignment Align(string left, string right)
        => SlideAlignment.Of(left.Select(slide => slide.ToString()).ToList(), right.Select(slide => slide.ToString()).ToList());

    [Fact]
    public void IdenticalDecksHaveNoRows()
    {
        var alignment = Align("ABC", "ABC");

        Assert.Empty(alignment.Rows);
        Assert.Empty(alignment.Moves);
    }

    [Fact]
    public void AddedSlidesSitOppositeBlanks()
    {
        var alignment = Align("AB", "AxBy");

        Assert.Equal([new SlideRow(null, 2), new SlideRow(null, 4)], alignment.Rows);
        Assert.Empty(alignment.Moves);
    }

    [Fact]
    public void RemovedSlideSitsOppositeABlank()
    {
        Assert.Equal([new SlideRow(2, null)], Align("ABC", "AC").Rows);
    }

    [Fact]
    public void EditedSlidePairsWithItsNewVersion()
    {
        Assert.Equal([new SlideRow(2, 2)], Align("ABC", "AxC").Rows);
    }

    [Fact]
    public void SlidesPushedDownByAnAdditionAreNotMoves()
    {
        var alignment = Align("ABC", "xABy");

        Assert.Equal([new SlideRow(null, 1), new SlideRow(3, 4)], alignment.Rows);
        Assert.Empty(alignment.Moves);
    }

    [Fact]
    public void MovedSlideIsHiddenAndReported()
    {
        var alignment = Align("ABC", "CAB");

        Assert.Empty(alignment.Rows);
        Assert.Equal([new SlideMove(3, 1)], alignment.Moves);
    }

    [Fact]
    public void MovedSlideDoesNotBreakPairingAroundIt()
    {
        var alignment = Align("ABCD", "DAxC");

        Assert.Equal([new SlideRow(2, 3)], alignment.Rows);
        Assert.Equal([new SlideMove(4, 1)], alignment.Moves);
    }

    [Fact]
    public void ExtraCopyOfADuplicateShowsAsAdded()
    {
        var alignment = Align("AB", "ABA");

        Assert.Equal([new SlideRow(null, 3)], alignment.Rows);
        Assert.Empty(alignment.Moves);
    }

    [Fact]
    public void EmptySidesAreAllowed()
    {
        Assert.Equal([new SlideRow(null, 1)], Align("", "A").Rows);
    }
}
