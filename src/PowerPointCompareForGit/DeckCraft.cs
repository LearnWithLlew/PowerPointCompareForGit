using System.Globalization;

namespace PowerPointCompareForGit;

internal static class DeckCraft
{
    private static string CliFileName => OperatingSystem.IsWindows() ? "deckcraft-cli.exe" : "deckcraft-cli";

    // Where DeckCraftSetup installs it, next to the settings file.
    internal static string InstallFolder() => Path.Combine(Path.GetDirectoryName(Settings.DefaultPath())!, "deckcraft");

    // configuredPath is the deckCraftCliPath setting, used first when it points at a file.
    // Then PATH, then what DeckCraftSetup installed.
    internal static string? Find(string? configuredPath = null)
    {
        if (configuredPath is not null && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var onPath = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder, CliFileName))
            .FirstOrDefault(File.Exists);
        return onPath ?? FindIn(InstallFolder());
    }

    // The archives keep it in different places, so this searches the whole folder.
    internal static string? FindIn(string folder)
        => Directory.Exists(folder) ? Directory.EnumerateFiles(folder, CliFileName, SearchOption.AllDirectories).FirstOrDefault() : null;

    internal static string NotFoundMessage()
        => $"deckcraft-cli was not found. Download it from https://github.com/storytold/deckcraft/releases/latest and put it on PATH (or set \"deckCraftCliPath\" in {Settings.DefaultPath()}, or use --renderer LibreOffice).";
}

internal sealed class DeckCraftRenderer(string cli) : ISlideRenderer
{
    // deckcraft-cli scales by pixels per point. 4/3 is LibreOffice's default export size, so both line up (a 4:3 deck is 960×720).
    private const double PixelsPerPoint = 4.0 / 3.0;

    public List<RgbaImage> RenderSlides(string deckPath, IReadOnlyList<int> slideNumbers, string work)
    {
        // Every launch reloads the deck, so draw them all in one launch when all are wanted.
        if (slideNumbers.SequenceEqual(Enumerable.Range(1, SlideSplitter.CountSlides(deckPath))))
        {
            var folder = Path.Combine(work, "all");
            RenderAll(deckPath, folder, work);
            return slideNumbers.Select(slideNumber => RgbaImage.LoadPng(PathInRenderAll(folder, slideNumber))).ToList();
        }

        return slideNumbers.Select(slideNumber => RenderSlide(deckPath, slideNumber, work)).ToList();
    }

    private void RenderAll(string deckPath, string outputFolder, string work)
    {
        string[] arguments = ["render", deckPath, "--all", "--scale", PixelsPerPoint.ToString(CultureInfo.InvariantCulture), outputFolder];
        var result = ProcessRunner.Run(cli, arguments, work);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"deckcraft-cli failed (exit code {result.ExitCode}): {result.Error.Trim()}");
        }
    }

    // RenderAll names them slide-01.png, slide-02.png... slide-99.png, slide-100.png.
    internal static string PathInRenderAll(string outputFolder, int slideNumber)
        => Path.Combine(outputFolder, $"slide-{slideNumber.ToString("D2", CultureInfo.InvariantCulture)}.png");

    // slideNumber is 1-based; deckcraft-cli counts from 0.
    private RgbaImage RenderSlide(string deckPath, int slideNumber, string work)
    {
        var png = Path.Combine(work, $"slide{slideNumber:D4}.png");
        string[] arguments =
        [
            "render", deckPath,
            "--slide", (slideNumber - 1).ToString(CultureInfo.InvariantCulture),
            "--scale", PixelsPerPoint.ToString(CultureInfo.InvariantCulture),
            png
        ];
        var result = ProcessRunner.Run(cli, arguments, work);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"deckcraft-cli failed on slide {slideNumber} (exit code {result.ExitCode}): {result.Error.Trim()}");
        }

        return RgbaImage.LoadPng(png);
    }
}
