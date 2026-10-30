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

        DeckRenderer.Render(new LibreOfficeRenderer(soffice), TestFolder.ThreeSlideDeck, output);

        var image = RgbaImage.LoadPng(output);
        Assert.Equal(960, image.Width);
        Assert.Equal(3 * (SlideStitcher.BandHeight + 720), image.Height);
    }

    // Experiment: deckcraft-cli is optional and CI doesn't install it, so this only runs where it is on PATH.
    [Fact]
    public void DeckCraftRendersTheSameSizeAsLibreOffice()
    {
        var cli = DeckCraft.Find();
        if (cli is null)
        {
            return;
        }

        using var folder = new TestFolder();
        var output = folder.Combine("three-slides.png");

        DeckRenderer.Render(new DeckCraftRenderer(cli), TestFolder.ThreeSlideDeck, output);

        var image = RgbaImage.LoadPng(output);
        Assert.Equal(960, image.Width);
        Assert.Equal(3 * (SlideStitcher.BandHeight + 720), image.Height);
    }
}
