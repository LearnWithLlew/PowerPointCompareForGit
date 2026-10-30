namespace PowerPointCompareForGit.Tests;

// Runs real LibreOffice, which must be installed (CI installs it).
public class CommitPreviewsTests
{
    private const int SlideHeight = 720;

    [Fact]
    public void RendersEachCommitSeparately()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var threeSlides = repository.ResolveCommit("HEAD");
        SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path);
        File.Copy(folder.Combine("slide0001.pptx"), deckPath, overwrite: true);
        GitRepositoryTests.Git(folder.Path, "commit", "-q", "-am", "one slide");
        var oneSlide = repository.ResolveCommit("HEAD");

        var threeSlidePreview = CommitPreviews.Render(repository, "decks/my.pptx", threeSlides);
        var oneSlidePreview = CommitPreviews.Render(repository, "decks/my.pptx", oneSlide);

        Assert.Equal(3 * (SlideStitcher.BandHeight + SlideHeight), RgbaImage.LoadPng(threeSlidePreview).Height);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(oneSlidePreview).Height);
    }

    [Fact]
    public void ReusesCachedPreview()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");
        var cached = PreviewPaths.For(repository.Root, "decks/my.pptx", sha);
        Directory.CreateDirectory(Path.GetDirectoryName(cached)!);
        File.WriteAllText(cached, "already rendered");

        Assert.Equal(cached, CommitPreviews.Render(repository, "decks/my.pptx", sha));
        Assert.Equal("already rendered", File.ReadAllText(cached));
    }

    [Fact]
    public void DeckMissingFromCommitFails()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");

        var error = Assert.Throws<InvalidOperationException>(() => CommitPreviews.Render(repository, "decks/other.pptx", sha));
        Assert.Equal($"decks/other.pptx does not exist in commit {sha[..7]}.", error.Message);
    }

    [Fact]
    public void WorkingCopyIsRenderedFromDiskEveryTime()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path);
        File.Copy(folder.Combine("slide0001.pptx"), deckPath, overwrite: true);
        var stale = PreviewPaths.For(repository.Root, "decks/my.pptx", PreviewPaths.WorkingCopy);
        Directory.CreateDirectory(Path.GetDirectoryName(stale)!);
        File.WriteAllText(stale, "stale");

        var preview = CommitPreviews.RenderWorkingCopy(repository, "decks/my.pptx", deckPath);

        Assert.Equal(stale, preview);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(preview).Height);
    }
}
