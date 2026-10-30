namespace PowerPointCompareForGit.Tests;

// Runs real LibreOffice, which must be installed (CI installs it).
public class DeckRendererTests
{
    [Fact]
    public void RendersEverySlideIntoOneTallPng()
    {
        var soffice = LibreOffice.Find();
        Assert.True(soffice is not null, LibreOffice.NotFoundMessage());
        using var folder = new TestFolder();
        var output = folder.Combine("previews", "three-slides.png");

        DeckRenderer.Render(soffice, TestFolder.ThreeSlideDeck, output);

        var image = RgbaImage.LoadPng(output);
        Assert.Equal(960, image.Width);
        Assert.Equal(3 * (SlideStitcher.BandHeight + 720), image.Height);
    }
}
