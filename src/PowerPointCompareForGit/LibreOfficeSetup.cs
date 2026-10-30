using System.Diagnostics;

namespace PowerPointCompareForGit;

internal sealed record Installer(string Name, string FileName, string[] Arguments)
{
    internal string CommandText => $"{FileName} {string.Join(' ', Arguments)}";
}

internal static class LibreOfficeSetup
{
    // brew on Mac, winget on Windows. Linux has none: installing there needs sudo, so the user does it.
    internal static Installer? InstallerForThisOs()
        => OperatingSystem.IsMacOS() ? new Installer("brew", "brew", ["install", "--cask", "libreoffice"])
            : OperatingSystem.IsWindows() ? new Installer("winget", "winget", ["install", "TheDocumentFoundation.LibreOffice"])
            : null;

    // Returns the soffice path, offering to install LibreOffice when it is missing.
    // With piped input, or no installer for this OS, it fails with the install command instead of prompting.
    internal static string Resolve(Func<string?> find, Installer? installer, TextReader input, TextWriter prompt, bool inputRedirected, Action<Installer> install)
    {
        if (find() is { } found)
        {
            return found;
        }

        if (installer is null || inputRedirected)
        {
            throw new InvalidOperationException(LibreOffice.NotFoundMessage());
        }

        prompt.WriteLine("LibreOffice was not found.");
        prompt.WriteLine($"1. Install via {installer.Name} ({installer.CommandText})");
        prompt.WriteLine($"2. Cancel (or set libreOfficePath in {Settings.DefaultPath()})");
        prompt.Write("Choice: ");
        if (input.ReadLine()?.Trim() != "1")
        {
            throw new InvalidOperationException($"Cancelled. {LibreOffice.NotFoundMessage()}");
        }

        install(installer);
        return find() ?? throw new InvalidOperationException($"LibreOffice still wasn't found after installing. {LibreOffice.NotFoundMessage()}");
    }

    // Runs the installer in this console so the user sees its progress and any prompts.
    internal static void RunInstaller(Installer installer)
    {
        var startInfo = new ProcessStartInfo(installer.FileName) { UseShellExecute = false };
        foreach (var argument in installer.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {installer.FileName}.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{installer.CommandText} failed (exit code {process.ExitCode}).");
        }
    }
}
