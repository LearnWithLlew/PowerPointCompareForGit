using System.Globalization;

namespace PowerPointCompareForGit;

internal static class DeckCraft
{
    // deckcraft-cli scales by pixels per point. 4/3 is LibreOffice's default export size, so both line up (a 4:3 deck is 960×720).
    private const double PixelsPerPoint = 4.0 / 3.0;

    // configuredPath is the deckCraftCliPath setting, used first when it points at a file.
    internal static string? Find(string? configuredPath = null)
    {
        if (configuredPath is not null && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var name = OperatingSystem.IsWindows() ? "deckcraft-cli.exe" : "deckcraft-cli";
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder, name))
            .FirstOrDefault(File.Exists);
    }

    internal static string NotFoundMessage()
        => $"deckcraft-cli was not found. Download it from https://github.com/storytold/deckcraft/releases/latest and put it on PATH (or set \"deckCraftCliPath\" in {Settings.DefaultPath()}).";

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
