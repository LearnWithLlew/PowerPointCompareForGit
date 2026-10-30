namespace PowerPointCompareForGit;

internal static class LibreOffice
{
    private static readonly string[] StandardLocations =
    [
        "/Applications/LibreOffice.app/Contents/MacOS/soffice",
        @"C:\Program Files\LibreOffice\program\soffice.exe"
    ];

    internal static string? Find()
    {
        var names = OperatingSystem.IsWindows() ? new[] { "soffice.exe", "soffice.com" } : ["soffice"];
        var onPath = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(folder => names.Select(name => Path.Combine(folder, name)))
            .FirstOrDefault(File.Exists);
        return onPath ?? StandardLocations.FirstOrDefault(File.Exists);
    }

    internal static string NotFoundMessage()
    {
        var install = OperatingSystem.IsMacOS() ? "brew install --cask libreoffice"
            : OperatingSystem.IsWindows() ? "winget install TheDocumentFoundation.LibreOffice"
            : "sudo apt install libreoffice";
        return $"LibreOffice was not found. Install it with: {install}";
    }

    // Converts every file to PNG in a single launch.
    // A private profile folder keeps this working while the user has LibreOffice open.
    internal static void ConvertToPng(string soffice, IReadOnlyList<string> files, string outputFolder, string profileFolder)
    {
        string[] arguments =
        [
            $"-env:UserInstallation={new Uri(Path.GetFullPath(profileFolder)).AbsoluteUri}",
            "--headless", "--convert-to", "png", "--outdir", outputFolder,
            .. files
        ];
        var result = ProcessRunner.Run(soffice, arguments, outputFolder);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"LibreOffice failed (exit code {result.ExitCode}): {result.Error.Trim()}");
        }
    }
}
