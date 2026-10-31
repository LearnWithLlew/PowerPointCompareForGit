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

    [Fact]
    public void NothingChangedSaysSo()
    {
        Assert.Equal(["No visible slide changes between abc1234 and def5678."], Align("AB", "AB").Messages("abc1234", "def5678"));
    }

    [Fact]
    public void OnlyReorderedSaysSoAndListsTheMoves()
    {
        Assert.Equal(["Slides were reordered, but none changed.", "Slides moved but unchanged: 3→1."], Align("ABC", "CAB").Messages("a", "b"));
    }

    [Fact]
    public void MovesAreReportedAlongsideChanges()
    {
        Assert.Equal(["Slides moved but unchanged: 4→1."], Align("ABCD", "DAxC").Messages("a", "b"));
    }

    [Fact]
    public void ChangesWithoutMovesSayNothing()
    {
        Assert.Empty(Align("ABC", "AxC").Messages("a", "b"));
    }

    [Fact]
    public void LongMoveListIsCutOffWithACount()
    {
        var moves = Enumerable.Range(1, 12).Select(slide => new SlideMove(slide, slide + 20)).ToList();

        var message = Assert.Single(new SlideAlignment([new SlideRow(1, 1)], moves).Messages("a", "b"));

        Assert.Equal("Slides moved but unchanged: 1→21, 2→22, 3→23, 4→24, 5→25, 6→26, 7→27, 8→28, 9→29, 10→30 ...and 2 more.", message);
    }
}
