namespace PowerPointCompareForGit;

internal static class DeckRenderer
{
    // Renders every slide of the deck and stacks them into one PNG at outputPath.
    // The PNG is written to a temp file first, so a failed render never leaves a half-written preview in the cache.
    internal static void Render(ISlideRenderer renderer, string deckPath, string outputPath)
        => InWorkFolder(work =>
        {
            var slideCount = SlideSplitter.CountSlides(deckPath);
            if (slideCount == 0)
            {
                throw new InvalidOperationException($"{deckPath} has no slides.");
            }

            var slides = renderer.RenderSlides(deckPath, Enumerable.Range(1, slideCount).ToList(), work);
            SaveInto(SlideStitcher.Stitch(slides), outputPath, work);
        });

    // Renders the range of slides and stacks them into one PNG at outputPath, with bands showing the real slide numbers.
    internal static void RenderRange(ISlideRenderer renderer, string deckPath, PageRange pages, string outputPath)
        => InWorkFolder(work =>
        {
            var slides = renderer.RenderSlides(deckPath, pages.Pages, work);
            SaveInto(SlideStitcher.Stitch(slides, pages.First), outputPath, work);
        });

    // Renders each of the given slides to its own PNG, with no band. The file name carries the slide number.
    internal static void RenderPages(ISlideRenderer renderer, string deckPath, IReadOnlyList<int> pages, Func<int, string> outputPathFor)
        => InWorkFolder(work =>
        {
            var slides = renderer.RenderSlides(deckPath, pages, work);
            for (var i = 0; i < pages.Count; i++)
            {
                SaveInto(slides[i], outputPathFor(pages[i]), work);
            }
        });

    private static void InWorkFolder(Action<string> action)
    {
        var work = Directory.CreateTempSubdirectory("pptx-render-");
        try
        {
            action(work.FullName);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static void SaveInto(RgbaImage image, string outputPath, string work)
    {
        var temporary = Path.Combine(work, "output.png");
        image.SavePng(temporary);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.Move(temporary, outputPath, overwrite: true);
    }
}
