using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PowerPointCompareForGit;

internal static class DeckCraftSetup
{
    private const string MacApp = "/Applications/DeckCraft.app";

    // Returns the deckcraft-cli path, offering to install DeckCraft and deckcraft-cli when the CLI is missing.
    // Only the CLI renders, so a missing app alone never prompts. With piped input it fails with instructions instead.
    internal static string Resolve(Func<string?> find, UserConsole console, Action install)
    {
        if (find() is { } found)
        {
            return found;
        }

        if (console.InputRedirected)
        {
            throw new InvalidOperationException(DeckCraft.NotFoundMessage());
        }

        console.Prompt.WriteLine("deckcraft-cli was not found.");
        console.Prompt.WriteLine($"1. Install DeckCraft and deckcraft-cli (from {DeckCraftRelease.Releases})");
        console.Prompt.WriteLine($"2. Cancel (or use --renderer LibreOffice, or set deckCraftCliPath in {Settings.DefaultPath()})");
        if (console.Ask("Choice: ") != "1")
        {
            throw new InvalidOperationException($"Cancelled. {DeckCraft.NotFoundMessage()}");
        }

        install();
        return find() ?? throw new InvalidOperationException($"deckcraft-cli still wasn't found after installing. {DeckCraft.NotFoundMessage()}");
    }

    internal static void VerifyHash(string file, string expectedHash)
    {
        using var stream = File.OpenRead(file);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{Path.GetFileName(file)} failed its checksum, so it was not installed.");
        }
    }

    internal static void Unpack(string archive, string folder)
    {
        Directory.CreateDirectory(folder);
        if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archive, folder);
            return;
        }

        using var file = File.OpenRead(archive);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gzip, folder, overwriteFiles: true);
    }

    // Installs into DeckCraft.InstallFolder(), replacing what is there, then adds the Mac app if it is missing.
    // A failed app install is only a warning, because rendering needs just the CLI.
    internal static void Install(TextWriter progress)
    {
        var os = OperatingSystem.IsMacOS() ? OSPlatform.OSX : OperatingSystem.IsWindows() ? OSPlatform.Windows : OSPlatform.Linux;
        InstallCli(os, progress);
        if (os == OSPlatform.OSX)
        {
            InstallMacApp(progress);
        }
    }

    private static void InstallCli(OSPlatform os, TextWriter progress)
    {
        using var client = DeckCraftRelease.CreateClient();
        var asset = DeckCraftRelease.Latest(client, os);
        var work = Directory.CreateTempSubdirectory("deckcraft-install-");
        try
        {
            progress.WriteLine($"Downloading {asset.Name}...");
            ReplaceInstallFolder(DownloadAndUnpack(client, asset, work.FullName));
            progress.WriteLine($"Installed DeckCraft {asset.Version} in {DeckCraft.InstallFolder()}");
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static string DownloadAndUnpack(HttpClient client, DeckCraftRelease.Asset asset, string work)
    {
        var archive = Path.Combine(work, asset.Name);
        using (var source = client.GetStreamAsync(asset.Url).GetAwaiter().GetResult())
        using (var target = File.Create(archive))
        {
            source.CopyTo(target);
        }

        VerifyHash(archive, asset.ExpectedHash);
        var unpacked = Path.Combine(work, "unpacked");
        Unpack(archive, unpacked);
        MarkExecutable(unpacked);
        return unpacked;
    }

    private static void ReplaceInstallFolder(string unpacked)
    {
        var folder = DeckCraft.InstallFolder();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(folder)!);
        CopyFolder(unpacked, folder);
    }

    private static void MarkExecutable(string folder)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "deckcraft*", SearchOption.AllDirectories).Where(file => Path.GetExtension(file).Length == 0))
        {
            File.SetUnixFileMode(file, File.GetUnixFileMode(file) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
    }

    // Copied rather than moved, because the temp folder can be on another drive.
    private static void CopyFolder(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(destination, File.GetUnixFileMode(file));
            }
        }
    }

    private static void InstallMacApp(TextWriter progress)
    {
        if (Directory.Exists(MacApp))
        {
            return;
        }

        var brew = new Installer("brew", "brew", ["install", "--cask", "deckcraft"]);
        try
        {
            LibreOfficeSetup.RunInstaller(brew);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            progress.WriteLine($"Rendering will work, but the DeckCraft app wasn't installed ({ex.Message}). Get it with {brew.CommandText} or from {DeckCraftRelease.Releases}.");
        }
    }
}
