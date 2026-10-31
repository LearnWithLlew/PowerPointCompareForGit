namespace PowerPointCompareForGit;

// What draws the slides, chosen with --renderer.
internal enum Renderer
{
    LibreOffice,
    DeckCraft
}

// Draws the given 1-based slides of a deck, in order. work is an empty temp folder it may use.
internal interface ISlideRenderer
{
    List<RgbaImage> RenderSlides(string deckPath, IReadOnlyList<int> slideNumbers, string work);
}

internal sealed class LibreOfficeRenderer(string soffice) : ISlideRenderer
{
    public List<RgbaImage> RenderSlides(string deckPath, IReadOnlyList<int> slideNumbers, string work)
    {
        var splitFolder = Directory.CreateDirectory(Path.Combine(work, "split")).FullName;
        var pngFolder = Directory.CreateDirectory(Path.Combine(work, "png")).FullName;
        var profileFolder = Path.Combine(work, "profile");

        var copies = SlideSplitter.Split(deckPath, splitFolder, slideNumbers);
        LibreOffice.ConvertToPng(soffice, copies, pngFolder, profileFolder);

        return copies.Select(copy =>
        {
            var png = Path.Combine(pngFolder, Path.ChangeExtension(Path.GetFileName(copy), ".png"));
            return File.Exists(png)
                ? RgbaImage.LoadPng(png)
                : throw new InvalidOperationException($"LibreOffice did not produce {Path.GetFileName(png)}.");
        }).ToList();
    }
}

internal sealed class DeckCraftRenderer(string cli) : ISlideRenderer
{
    public List<RgbaImage> RenderSlides(string deckPath, IReadOnlyList<int> slideNumbers, string work)
    {
        // Every launch reloads the deck, so draw them all in one launch when all are wanted.
        if (slideNumbers.SequenceEqual(Enumerable.Range(1, SlideSplitter.CountSlides(deckPath))))
        {
            var folder = Path.Combine(work, "all");
            DeckCraft.RenderAll(cli, deckPath, folder, work);
            return slideNumbers.Select(slideNumber => RgbaImage.LoadPng(DeckCraft.PathInRenderAll(folder, slideNumber))).ToList();
        }

        return slideNumbers.Select(slideNumber =>
        {
            var png = Path.Combine(work, $"slide{slideNumber:D4}.png");
            DeckCraft.RenderSlide(cli, deckPath, slideNumber, png, work);
            return RgbaImage.LoadPng(png);
        }).ToList();
    }
}
