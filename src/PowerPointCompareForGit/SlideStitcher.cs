namespace PowerPointCompareForGit;

internal static class SlideStitcher
{
    internal const int BandHeight = 40;
    internal static readonly Rgba Background = new(255, 255, 255);
    internal static readonly Rgba BandColor = new(224, 224, 224);
    private static readonly Rgba LabelColor = new(64, 64, 64);

    // Stacks the slides vertically, each under a gray band labeled "Slide N", counting from firstSlideNumber.
    // Narrower slides are padded with white.
    internal static RgbaImage Stitch(IReadOnlyList<RgbaImage> slides, int firstSlideNumber = 1)
    {
        if (slides.Count == 0)
        {
            throw new ArgumentException("There are no slides to stitch.", nameof(slides));
        }

        var result = new RgbaImage(slides.Max(slide => slide.Width), slides.Sum(slide => BandHeight + slide.Height), Background);
        var top = 0;
        for (var i = 0; i < slides.Count; i++)
        {
            result.Fill(0, top, result.Width, BandHeight, BandColor);
            PixelFont.Draw(result, $"Slide {firstSlideNumber + i}", 10, top + (BandHeight - PixelFont.Height) / 2, LabelColor);
            top += BandHeight;

            result.Paste(slides[i], 0, top);
            top += slides[i].Height;
        }

        return result;
    }
}
