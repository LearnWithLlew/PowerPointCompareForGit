using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using static PowerPointCompareForGit.Tests.CommitPreviewsTests;
using A = DocumentFormat.OpenXml.Drawing;

namespace PowerPointCompareForGit.Tests;

public class CommitPreviewsSelectiveTests
{
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
        FakeCachedPreviews(cachedLeft, cachedRight);

        new CommitPreviews(deck).RenderSelective(before, after);

        AssertNotRerendered(cachedLeft, cachedRight);
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
}
