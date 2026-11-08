namespace PowerPointCompareForGit.Tests;

// Runs real LibreOffice, which must be installed (CI installs it).
public class CommitPreviewsTests
{
    internal const int SlideHeight = 720;

    [Fact]
    public void RendersEachCommitSeparately()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var threeSlides = deck.Repository.ResolveCommit("HEAD");
        SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path);
        File.Copy(folder.Combine("slide0001.pptx"), deckPath, overwrite: true);
        GitRepositoryTests.Git(folder.Path, "commit", "-q", "-am", "one slide");
        var oneSlide = deck.Repository.ResolveCommit("HEAD");

        var threeSlidePreview = new CommitPreviews(deck).RenderCommit(threeSlides);
        var oneSlidePreview = new CommitPreviews(deck).RenderCommit(oneSlide);

        Assert.Equal(3 * (SlideStitcher.BandHeight + SlideHeight), RgbaImage.LoadPng(threeSlidePreview).Height);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(oneSlidePreview).Height);
    }

    [Fact]
    public void ReusesCachedPreview()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");
        var cached = PathsOf(deck).For(sha);
        FakeCachedPreviews(cached);

        Assert.Equal(cached, new CommitPreviews(deck).RenderCommit(sha));
        AssertNotRerendered(cached);
    }

    [Fact]
    public void DeckMissingFromCommitFails()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");

        var error = Assert.Throws<InvalidOperationException>(() => new CommitPreviews(DeckInRepo.At(folder.Combine("decks", "other.pptx"))).RenderCommit(sha));
        Assert.Equal($"decks/other.pptx does not exist in commit {sha[..7]}.", error.Message);
    }

    [Fact]
    public void WorkingCopyIsRenderedFromDiskEveryTime()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path);
        File.Copy(folder.Combine("slide0001.pptx"), deckPath, overwrite: true);
        var stale = PathsOf(deck).For(PreviewPaths.WorkingCopy);
        Directory.CreateDirectory(Path.GetDirectoryName(stale)!);
        File.WriteAllText(stale, "stale");

        var preview = new CommitPreviews(deck).RenderWorkingCopy();

        Assert.Equal(stale, preview);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(preview).Height);
    }

    internal static PreviewPaths PathsOf(DeckInRepo deck) => new(deck.Repository.Root, deck.RelativePath);

    internal static void FakeCachedPreviews(params string[] paths)
    {
        foreach (var path in paths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "already rendered");
        }
    }

    internal static void AssertNotRerendered(params string[] cachedPreviews)
        => Assert.All(cachedPreviews, cached => Assert.Equal("already rendered", File.ReadAllText(cached)));
}
