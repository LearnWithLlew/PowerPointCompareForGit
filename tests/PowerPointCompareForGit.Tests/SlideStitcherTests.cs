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
    public void AlignedRowsSitLevelWithBlanksOppositeMissingSlides()
    {
        var (left, right) = SlideStitcher.StitchAligned(
        [
            (null, new NumberedSlide(21, new RgbaImage(100, 50, Blue))),
            (new NumberedSlide(23, new RgbaImage(100, 50, Red)), new NumberedSlide(24, new RgbaImage(100, 50, Blue)))
        ]);

        const int band = SlideStitcher.BandHeight;
        Assert.Equal((100, 2 * (band + 50)), (left.Width, left.Height));
        Assert.Equal((100, 2 * (band + 50)), (right.Width, right.Height));
        Assert.Equal(SlideStitcher.Background, left[99, 0]);
        Assert.Equal(SlideStitcher.Background, left[50, band + 25]);
        Assert.Equal(SlideStitcher.BandColor, right[99, 0]);
        Assert.Equal(Blue, right[50, band + 25]);
        Assert.Equal(Red, left[50, band + 50 + band + 25]);
        Assert.Equal(Blue, right[50, band + 50 + band + 25]);
    }

    [Fact]
    public void AlignedRowIsAsTallAsItsTallerSlide()
    {
        var (left, right) = SlideStitcher.StitchAligned(
        [
            (new NumberedSlide(1, new RgbaImage(100, 50, Red)), new NumberedSlide(1, new RgbaImage(200, 80, Blue))),
            (new NumberedSlide(2, new RgbaImage(100, 50, Red)), new NumberedSlide(2, new RgbaImage(200, 80, Blue)))
        ]);

        const int band = SlideStitcher.BandHeight;
        Assert.Equal((200, 2 * (band + 80)), (left.Width, left.Height));
        Assert.Equal((200, 2 * (band + 80)), (right.Width, right.Height));
        Assert.Equal(Red, left[50, band + 80 + band + 25]);
        Assert.Equal(Blue, right[50, band + 80 + band + 25]);
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
    public void BandLabelStartsAtFirstSlideNumber()
    {
        static IEnumerable<Rgba> Band(RgbaImage image)
            => Enumerable.Range(0, 200).SelectMany(x => Enumerable.Range(0, SlideStitcher.BandHeight).Select(y => image[x, y]));

        var fromOne = SlideStitcher.Stitch([new RgbaImage(200, 10, Red)]);
        var fromFive = SlideStitcher.Stitch([new RgbaImage(200, 10, Red)], firstSlideNumber: 5);

        Assert.NotEqual(Band(fromOne), Band(fromFive));
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
