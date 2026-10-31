namespace PowerPointCompareForGit;

internal readonly record struct NumberedSlide(int Number, RgbaImage Image);

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
            DrawBand(result, firstSlideNumber + i, top);
            top += BandHeight;

            result.Paste(slides[i], 0, top);
            top += slides[i].Height;
        }

        return result;
    }

    // Stacks both sides row by row, so the two images have the same size and each row sits level.
    // A row is as tall as its taller slide, and a missing slide leaves that row blank, band included.
    internal static (RgbaImage Left, RgbaImage Right) StitchAligned(IReadOnlyList<(NumberedSlide? Left, NumberedSlide? Right)> rows)
    {
        if (rows.Count == 0)
        {
            throw new ArgumentException("There are no rows to stitch.", nameof(rows));
        }

        var slides = rows.SelectMany(row => new[] { row.Left, row.Right }).OfType<NumberedSlide>().ToList();
        var width = slides.Max(slide => slide.Image.Width);
        var height = rows.Sum(row => BandHeight + Math.Max(row.Left?.Image.Height ?? 0, row.Right?.Image.Height ?? 0));
        var left = new RgbaImage(width, height, Background);
        var right = new RgbaImage(width, height, Background);

        var top = 0;
        foreach (var row in rows)
        {
            DrawSlide(left, row.Left, top);
            DrawSlide(right, row.Right, top);
            top += BandHeight + Math.Max(row.Left?.Image.Height ?? 0, row.Right?.Image.Height ?? 0);
        }

        return (left, right);
    }

    private static void DrawSlide(RgbaImage target, NumberedSlide? slide, int top)
    {
        if (slide is { } drawn)
        {
            DrawBand(target, drawn.Number, top);
            target.Paste(drawn.Image, 0, top + BandHeight);
        }
    }

    private static void DrawBand(RgbaImage target, int slideNumber, int top)
    {
        target.Fill(0, top, target.Width, BandHeight, BandColor);
        PixelFont.Draw(target, $"Slide {slideNumber}", 10, top + (BandHeight - PixelFont.Height) / 2, LabelColor);
    }
}
