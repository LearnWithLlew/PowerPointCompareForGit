namespace PowerPointCompareForGit;

internal static class DeckRenderer
{
    // Renders every slide of the deck and stacks them into one PNG at outputPath.
    // The PNG is written to a temp file first, so a failed render never leaves a half-written preview in the cache.
    internal static void Render(string soffice, string deckPath, string outputPath)
    {
        var work = Directory.CreateTempSubdirectory("pptx-render-");
        try
        {
            var splitFolder = Directory.CreateDirectory(Path.Combine(work.FullName, "split")).FullName;
            var pngFolder = Directory.CreateDirectory(Path.Combine(work.FullName, "png")).FullName;
            var profileFolder = Path.Combine(work.FullName, "profile");

            var copies = SlideSplitter.Split(deckPath, splitFolder);
            if (copies.Count == 0)
            {
                throw new InvalidOperationException($"{deckPath} has no slides.");
            }

            LibreOffice.ConvertToPng(soffice, copies, pngFolder, profileFolder);

            var slides = copies.Select(copy =>
            {
                var png = Path.Combine(pngFolder, Path.ChangeExtension(Path.GetFileName(copy), ".png"));
                return File.Exists(png)
                    ? RgbaImage.LoadPng(png)
                    : throw new InvalidOperationException($"LibreOffice did not produce {Path.GetFileName(png)}.");
            }).ToList();

            var temporary = Path.Combine(work.FullName, "stitched.png");
            SlideStitcher.Stitch(slides).SavePng(temporary);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.Move(temporary, outputPath, overwrite: true);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }
}
