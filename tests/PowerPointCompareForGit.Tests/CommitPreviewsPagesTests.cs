using static PowerPointCompareForGit.Tests.CommitPreviewsTests;

namespace PowerPointCompareForGit.Tests;

public class CommitPreviewsPagesTests
{
    [Fact]
    public void RangeRendersOnePngPerPageClampedToTheDeck()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");

        var previews = new CommitPreviews(deck).RenderPages(sha, new PageRange(2, 25));

        Assert.Equal(
            [PathsOf(deck).ForPage(sha, 2), PathsOf(deck).ForPage(sha, 3)],
            previews);
        Assert.All(previews, preview => Assert.Equal(SlideHeight, RgbaImage.LoadPng(preview).Height));
    }

    [Fact]
    public void PagePastTheEndRendersTheLastSlide()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");

        var preview = Assert.Single(new CommitPreviews(deck).RenderPages(sha, new PageRange(40, 40)));

        Assert.Equal(PathsOf(deck).ForPage(sha, 3), preview);
    }

    [Fact]
    public void ReusesCachedPage()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");
        var cached = PathsOf(deck).ForPage(sha, 2);
        FakeCachedPreviews(cached);

        var preview = Assert.Single(new CommitPreviews(deck).RenderPages(sha, new PageRange(2, 2)));

        Assert.Equal(cached, preview);
        AssertNotRerendered(cached);
    }

    [Fact]
    public void CompareStitchesARange()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");

        var preview = new CommitPreviews(deck).RenderForCompare(sha, new PageRange(2, 25));

        Assert.Equal(PathsOf(deck).ForPages(sha, new PageRange(2, 3)), preview);
        Assert.Equal(2 * (SlideStitcher.BandHeight + SlideHeight), RgbaImage.LoadPng(preview).Height);
    }

    [Fact]
    public void CompareOfOnePageUsesThePageFile()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);

        var preview = new CommitPreviews(deck).RenderForCompare(PreviewPaths.WorkingCopy, new PageRange(40, 50));

        Assert.Equal(PathsOf(deck).ForPage(PreviewPaths.WorkingCopy, 3), preview);
        Assert.Equal(SlideHeight, RgbaImage.LoadPng(preview).Height);
    }
}
