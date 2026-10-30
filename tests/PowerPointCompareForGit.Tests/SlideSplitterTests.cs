using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace PowerPointCompareForGit.Tests;

public class SlideSplitterTests
{
    [Fact]
    public void EachCopyKeepsOnlyItsOwnSlide()
    {
        using var folder = new TestFolder();

        var copies = SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path);

        Assert.Equal(["slide0001.pptx", "slide0002.pptx", "slide0003.pptx"], copies.Select(Path.GetFileName));
        for (var i = 0; i < copies.Count; i++)
        {
            using var deck = PresentationDocument.Open(copies[i], false);
            var presentationPart = deck.PresentationPart!;
            var slideId = Assert.Single(presentationPart.Presentation!.SlideIdList!.Elements<SlideId>());
            var slide = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
            Assert.Contains($"Slide {i + 1}", slide.Slide!.InnerText);
            Assert.Single(presentationPart.SlideParts);
        }
    }

    [Fact]
    public void SplitsOnlyTheGivenSlides()
    {
        using var folder = new TestFolder();

        var copies = SlideSplitter.Split(TestFolder.ThreeSlideDeck, folder.Path, [2, 3]);

        Assert.Equal(["slide0002.pptx", "slide0003.pptx"], copies.Select(Path.GetFileName));
        using var deck = PresentationDocument.Open(copies[0], false);
        var presentationPart = deck.PresentationPart!;
        var slideId = Assert.Single(presentationPart.Presentation!.SlideIdList!.Elements<SlideId>());
        var slide = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
        Assert.Contains("Slide 2", slide.Slide!.InnerText);
    }

    [Fact]
    public void CountsSlides()
    {
        Assert.Equal(3, SlideSplitter.CountSlides(TestFolder.ThreeSlideDeck));
    }
}
