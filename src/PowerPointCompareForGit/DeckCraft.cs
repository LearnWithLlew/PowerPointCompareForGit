using System.Globalization;

namespace PowerPointCompareForGit;

internal static class DeckCraft
{
    // deckcraft-cli scales by pixels per point. 4/3 is LibreOffice's default export size, so both line up (a 4:3 deck is 960×720).
    private const double PixelsPerPoint = 4.0 / 3.0;

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

    internal static void RenderAll(string cli, string deckPath, string outputFolder, string workingDirectory)
    {
        string[] arguments = ["render", deckPath, "--all", "--scale", PixelsPerPoint.ToString(CultureInfo.InvariantCulture), outputFolder];
        var result = ProcessRunner.Run(cli, arguments, workingDirectory);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"deckcraft-cli failed (exit code {result.ExitCode}): {result.Error.Trim()}");
        }
    }

    // RenderAll names them slide-01.png, slide-02.png... slide-99.png, slide-100.png.
    internal static string PathInRenderAll(string outputFolder, int slideNumber)
        => Path.Combine(outputFolder, $"slide-{slideNumber.ToString("D2", CultureInfo.InvariantCulture)}.png");

    // slideNumber is 1-based; deckcraft-cli counts from 0.
    internal static void RenderSlide(string cli, string deckPath, int slideNumber, string outputPath, string workingDirectory)
    {
        string[] arguments =
        [
            "render", deckPath,
            "--slide", (slideNumber - 1).ToString(CultureInfo.InvariantCulture),
            "--scale", PixelsPerPoint.ToString(CultureInfo.InvariantCulture),
            outputPath
        ];
        var result = ProcessRunner.Run(cli, arguments, workingDirectory);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"deckcraft-cli failed on slide {slideNumber} (exit code {result.ExitCode}): {result.Error.Trim()}");
        }
    }
}
