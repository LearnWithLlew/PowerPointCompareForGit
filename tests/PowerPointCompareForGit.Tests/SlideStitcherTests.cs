namespace PowerPointCompareForGit.Tests;

public class SlideStitcherTests
{
    private static readonly Rgba Red = new(255, 0, 0);
    private static readonly Rgba Blue = new(0, 0, 255);

    [Fact]
    public void StacksSlidesUnderLabeledBands()
    {
        var stitched = SlideStitcher.Stitch([new RgbaImage(100, 50, Red), new RgbaImage(80, 30, Blue)]);

        const int band = SlideStitcher.BandHeight;
        Assert.Equal(100, stitched.Width);
        Assert.Equal(band + 50 + band + 30, stitched.Height);
        Assert.Equal(SlideStitcher.BandColor, stitched[99, 0]);
        Assert.Equal(Red, stitched[50, band + 25]);
        Assert.Equal(SlideStitcher.BandColor, stitched[99, band + 50]);
        Assert.Equal(Blue, stitched[40, band + 50 + band + 15]);
    }

    [Fact]
    public void NarrowerSlidesArePaddedWithWhite()
    {
        var stitched = SlideStitcher.Stitch([new RgbaImage(100, 50, Red), new RgbaImage(80, 30, Blue)]);

        Assert.Equal(SlideStitcher.Background, stitched[90, SlideStitcher.BandHeight + 50 + SlideStitcher.BandHeight + 15]);
    }

    [Fact]
    public void BandIsLabeled()
    {
        var stitched = SlideStitcher.Stitch([new RgbaImage(200, 10, Red)]);

        var labelPixels = Enumerable.Range(0, 200)
            .SelectMany(x => Enumerable.Range(0, SlideStitcher.BandHeight).Select(y => stitched[x, y]))
            .Count(pixel => pixel != SlideStitcher.BandColor);
        Assert.True(labelPixels > 0, "Expected the label text to be drawn in the band.");
    }

    [Fact]
    public void SavesAndLoadsPng()
    {
        using var folder = new TestFolder();
        var path = folder.Combine("stitched.png");

        SlideStitcher.Stitch([new RgbaImage(10, 10, Red)]).SavePng(path);
        var loaded = RgbaImage.LoadPng(path);

        Assert.Equal(10, loaded.Width);
        Assert.Equal(Red, loaded[5, SlideStitcher.BandHeight + 5]);
    }
}
