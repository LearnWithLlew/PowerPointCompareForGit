using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

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
    public void SelectiveDrawsOnlyTheChangedSlideOfEachVersion()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");
        EditSecondSlide(deckPath);

        var previews = CommitPreviews.RenderSelective(repository, "decks/my.pptx", deckPath, sha, PreviewPaths.WorkingCopy);

        Assert.Equal([new SlideRow(2, 2)], previews.Alignment.Rows);
        var (left, right) = previews.Images!.Value;
        Assert.Equal(PreviewPaths.ForSelective(repository.Root, "decks/my.pptx", sha, PreviewPaths.WorkingCopy), left);
        Assert.Equal(PreviewPaths.ForSelective(repository.Root, "decks/my.pptx", PreviewPaths.WorkingCopy, sha), right);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(left).Height);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(right).Height);
    }

    [Fact]
    public void SelectiveWithNoChangesHasNoImages()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");

        var previews = CommitPreviews.RenderSelective(repository, "decks/my.pptx", deckPath, sha, PreviewPaths.WorkingCopy);

        Assert.Empty(previews.Alignment.Rows);
        Assert.Null(previews.Images);
    }

    [Fact]
    public void SelectiveReusesCachedPreviewsBetweenCommits()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var before = repository.ResolveCommit("HEAD");
        EditSecondSlide(deckPath);
        GitRepositoryTests.Git(folder.Path, "commit", "-q", "-am", "edit");
        var after = repository.ResolveCommit("HEAD");
        var cachedLeft = PreviewPaths.ForSelective(repository.Root, "decks/my.pptx", before, after);
        var cachedRight = PreviewPaths.ForSelective(repository.Root, "decks/my.pptx", after, before);
        Directory.CreateDirectory(Path.GetDirectoryName(cachedLeft)!);
        File.WriteAllText(cachedLeft, "already rendered");
        File.WriteAllText(cachedRight, "already rendered");

        CommitPreviews.RenderSelective(repository, "decks/my.pptx", deckPath, before, after);

        Assert.Equal("already rendered", File.ReadAllText(cachedLeft));
        Assert.Equal("already rendered", File.ReadAllText(cachedRight));
    }

    private static void EditSecondSlide(string deckPath)
    {
        using var deck = PresentationDocument.Open(deckPath, true);
        var presentationPart = deck.PresentationPart!;
        var slideId = presentationPart.Presentation!.SlideIdList!.Elements<SlideId>().ElementAt(1);
        var slide = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
        slide.Slide!.Descendants<A.Text>().First().Text = "Edited";
        slide.Slide.Save();
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
    public void RangeRendersOnePngPerPageClampedToTheDeck()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");

        var previews = CommitPreviews.RenderPages(repository, "decks/my.pptx", deckPath, sha, new PageRange(2, 25));

        Assert.Equal(
            [PreviewPaths.ForPage(repository.Root, "decks/my.pptx", sha, 2), PreviewPaths.ForPage(repository.Root, "decks/my.pptx", sha, 3)],
            previews);
        Assert.All(previews, preview => Assert.Equal(SlideHeight, RgbaImage.LoadPng(preview).Height));
    }

    [Fact]
    public void PagePastTheEndRendersTheLastSlide()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");

        var preview = Assert.Single(CommitPreviews.RenderPages(repository, "decks/my.pptx", deckPath, sha, new PageRange(40, 40)));

        Assert.Equal(PreviewPaths.ForPage(repository.Root, "decks/my.pptx", sha, 3), preview);
    }

    [Fact]
    public void ReusesCachedPage()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");
        var cached = PreviewPaths.ForPage(repository.Root, "decks/my.pptx", sha, 2);
        Directory.CreateDirectory(Path.GetDirectoryName(cached)!);
        File.WriteAllText(cached, "already rendered");

        var preview = Assert.Single(CommitPreviews.RenderPages(repository, "decks/my.pptx", deckPath, sha, new PageRange(2, 2)));

        Assert.Equal(cached, preview);
        Assert.Equal("already rendered", File.ReadAllText(cached));
    }

    [Fact]
    public void CompareStitchesARange()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.ResolveCommit("HEAD");

        var preview = CommitPreviews.RenderForCompare(repository, "decks/my.pptx", deckPath, sha, new PageRange(2, 25));

        Assert.Equal(PreviewPaths.ForPages(repository.Root, "decks/my.pptx", sha, new PageRange(2, 3)), preview);
        Assert.Equal(2 * (SlideStitcher.BandHeight + SlideHeight), RgbaImage.LoadPng(preview).Height);
    }

    [Fact]
    public void CompareOfOnePageUsesThePageFile()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);

        var preview = CommitPreviews.RenderForCompare(repository, "decks/my.pptx", deckPath, PreviewPaths.WorkingCopy, new PageRange(40, 50));

        Assert.Equal(PreviewPaths.ForPage(repository.Root, "decks/my.pptx", PreviewPaths.WorkingCopy, 3), preview);
        Assert.Equal(SlideHeight, RgbaImage.LoadPng(preview).Height);
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
