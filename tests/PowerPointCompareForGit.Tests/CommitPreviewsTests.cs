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
    public void SelectiveDrawsOnlyTheChangedSlideOfEachVersion()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");
        EditSecondSlide(deckPath);

        var previews = new CommitPreviews(deck).RenderSelective(sha, PreviewPaths.WorkingCopy);

        Assert.Equal([new SlideRow(2, 2)], previews.Alignment.Rows);
        var (left, right) = previews.Images!.Value;
        Assert.Equal(PathsOf(deck).ForSelective(sha, PreviewPaths.WorkingCopy), left);
        Assert.Equal(PathsOf(deck).ForSelective(PreviewPaths.WorkingCopy, sha), right);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(left).Height);
        Assert.Equal(SlideStitcher.BandHeight + SlideHeight, RgbaImage.LoadPng(right).Height);
    }

    [Fact]
    public void SelectiveWithNoChangesHasNoImages()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");

        var previews = new CommitPreviews(deck).RenderSelective(sha, PreviewPaths.WorkingCopy);

        Assert.Empty(previews.Alignment.Rows);
        Assert.Null(previews.Images);
    }

    [Fact]
    public void SelectiveReusesCachedPreviewsBetweenCommits()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var deck = DeckInRepo.At(deckPath);
        var before = deck.Repository.ResolveCommit("HEAD");
        EditSecondSlide(deckPath);
        GitRepositoryTests.Git(folder.Path, "commit", "-q", "-am", "edit");
        var after = deck.Repository.ResolveCommit("HEAD");
        var cachedLeft = PathsOf(deck).ForSelective(before, after);
        var cachedRight = PathsOf(deck).ForSelective(after, before);
        Directory.CreateDirectory(Path.GetDirectoryName(cachedLeft)!);
        File.WriteAllText(cachedLeft, "already rendered");
        File.WriteAllText(cachedRight, "already rendered");

        new CommitPreviews(deck).RenderSelective(before, after);

        Assert.Equal("already rendered", File.ReadAllText(cachedLeft));
        Assert.Equal("already rendered", File.ReadAllText(cachedRight));
    }

    private static PreviewPaths PathsOf(DeckInRepo deck) => new(deck.Repository.Root, deck.RelativePath);

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
        var deck = DeckInRepo.At(deckPath);
        var sha = deck.Repository.ResolveCommit("HEAD");
        var cached = PathsOf(deck).For(sha);
        Directory.CreateDirectory(Path.GetDirectoryName(cached)!);
        File.WriteAllText(cached, "already rendered");

        Assert.Equal(cached, new CommitPreviews(deck).RenderCommit(sha));
        Assert.Equal("already rendered", File.ReadAllText(cached));
    }

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
        Directory.CreateDirectory(Path.GetDirectoryName(cached)!);
        File.WriteAllText(cached, "already rendered");

        var preview = Assert.Single(new CommitPreviews(deck).RenderPages(sha, new PageRange(2, 2)));

        Assert.Equal(cached, preview);
        Assert.Equal("already rendered", File.ReadAllText(cached));
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
}
