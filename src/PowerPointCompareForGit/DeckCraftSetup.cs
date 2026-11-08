using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace PowerPointCompareForGit;

internal static class DeckCraftSetup
{
    private const string Releases = "https://github.com/storytold/deckcraft/releases";
    private const string MacApp = "/Applications/DeckCraft.app";

    // Returns the deckcraft-cli path, offering to install DeckCraft and deckcraft-cli when the CLI is missing.
    // Only the CLI renders, so a missing app alone never prompts. With piped input it fails with instructions instead.
    internal static string Resolve(Func<string?> find, TextReader input, TextWriter prompt, bool inputRedirected, Action install)
    {
        if (find() is { } found)
        {
            return found;
        }

        if (inputRedirected)
        {
            throw new InvalidOperationException(DeckCraft.NotFoundMessage());
        }

        prompt.WriteLine("deckcraft-cli was not found.");
        prompt.WriteLine($"1. Install DeckCraft and deckcraft-cli (from {Releases})");
        prompt.WriteLine($"2. Cancel (or use --renderer LibreOffice, or set deckCraftCliPath in {Settings.DefaultPath()})");
        prompt.Write("Choice: ");
        if (input.ReadLine()?.Trim() != "1")
        {
            throw new InvalidOperationException($"Cancelled. {DeckCraft.NotFoundMessage()}");
        }

        install();
        return find() ?? throw new InvalidOperationException($"deckcraft-cli still wasn't found after installing. {DeckCraft.NotFoundMessage()}");
    }

    // GitHub is asked at most once a day, so most runs make no network call.
    internal static bool UpdateCheckIsDue(string stampFile, DateTime utcNow)
        => !File.Exists(stampFile) || utcNow - File.GetLastWriteTimeUtc(stampFile) >= TimeSpan.FromDays(1);

    // deckcraft-cli --version prints "deckcraft-cli 0.13.1".
    internal static Version? InstalledVersion(string versionOutput)
        => Version.TryParse(versionOutput.Trim().Split(' ').Last(), out var version) ? version : null;

    // Asks whether to update when latest is newer. With piped input it only prints a notice.
    // Returns true when the user chose to update.
    internal static bool OfferUpdate(Version installed, string latest, TextReader input, TextWriter prompt, bool inputRedirected)
    {
        if (!Version.TryParse(latest, out var latestVersion) || latestVersion <= installed)
        {
            return false;
        }

        prompt.WriteLine($"DeckCraft {latest} is available (you have {installed}).");
        if (inputRedirected)
        {
            prompt.WriteLine("Run CreatePowerpointCheckpoint in a terminal to update it.");
            return false;
        }

        prompt.WriteLine("1. Update (this repo's cached DeckCraft previews are deleted, since the old version drew them)");
        prompt.WriteLine("2. Not now");
        prompt.Write("Choice: ");
        return input.ReadLine()?.Trim() == "1";
    }

    // Only updates the copy this tool installed; one on PATH or at deckCraftCliPath is the user's to manage.
    // Returns true when it updated. A failed check is silent, so working offline is unaffected.
    internal static bool CheckForUpdate(string cli, TextReader input, TextWriter prompt, bool inputRedirected)
    {
        var stampFile = Path.Combine(Path.GetDirectoryName(Settings.DefaultPath())!, "deckcraft-update-check.txt");
        if (!IsInstalledByUs(cli) || !UpdateCheckIsDue(stampFile, DateTime.UtcNow))
        {
            return false;
        }

        File.WriteAllText(stampFile, DateTime.UtcNow.ToString("O"));
        string latest;
        try
        {
            using var client = CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            latest = LatestVersion(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return false;
        }

        var installed = InstalledVersion(ProcessRunner.Run(cli, ["--version"], Path.GetTempPath()).OutputText);
        if (installed is null || !OfferUpdate(installed, latest, input, prompt, inputRedirected))
        {
            return false;
        }

        Install(prompt);
        return true;
    }

    private static bool IsInstalledByUs(string cli)
        => Path.GetFullPath(cli).StartsWith(Path.GetFullPath(DeckCraft.InstallFolder()) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    // The release file holding deckcraft-cli. On Windows and Linux it holds the app too; on Mac the app is separate.
    internal static string AssetName(string version, OSPlatform os, Architecture architecture)
    {
        if (os == OSPlatform.OSX)
        {
            return $"deckcraft-cli-{version}-macos-universal.zip";
        }

        if (os == OSPlatform.Windows)
        {
            return $"deckcraft-{version}-windows-{WindowsArchitectureName(architecture)}-portable.zip";
        }

        if (os == OSPlatform.Linux)
        {
            return $"deckcraft-{version}-linux-{LinuxArchitectureName(architecture)}.tar.gz";
        }

        throw new PlatformNotSupportedException($"Install DeckCraft from {Releases}.");
    }

    private static string WindowsArchitectureName(Architecture architecture) => architecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "x86",
        _ => throw new PlatformNotSupportedException($"DeckCraft has no Windows build for {architecture}. See {Releases}.")
    };

    private static string LinuxArchitectureName(Architecture architecture) => architecture switch
    {
        Architecture.X64 => "x86_64",
        Architecture.Arm64 => "aarch64",
        Architecture.RiscV64 => "riscv64",
        _ => throw new PlatformNotSupportedException($"DeckCraft has no Linux build for {architecture}. See {Releases}.")
    };

    // SHA256SUMS.txt has one "<hash>  <file name>" per line.
    internal static string? ExpectedHash(string checksums, string assetName)
        => checksums.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', 2, StringSplitOptions.TrimEntries))
            .FirstOrDefault(parts => parts.Length == 2 && parts[1].TrimStart('*') == assetName)?[0];

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
        using var client = CreateClient();
        var version = LatestVersion(client);
        var asset = AssetName(version, os, RuntimeInformation.OSArchitecture);
        var download = $"{Releases}/download/v{version}";
        var checksums = client.GetStringAsync($"{download}/SHA256SUMS.txt").GetAwaiter().GetResult();
        var expected = ExpectedHash(checksums, asset) ?? throw new InvalidOperationException($"SHA256SUMS.txt has no entry for {asset}, so it was not installed.");

        var work = Directory.CreateTempSubdirectory("deckcraft-install-");
        try
        {
            progress.WriteLine($"Downloading {asset}...");
            var archive = Path.Combine(work.FullName, asset);
            using (var source = client.GetStreamAsync($"{download}/{asset}").GetAwaiter().GetResult())
            using (var target = File.Create(archive))
            {
                source.CopyTo(target);
            }

            VerifyHash(archive, expected);
            var unpacked = Path.Combine(work.FullName, "unpacked");
            Unpack(archive, unpacked);
            MarkExecutable(unpacked);

            var folder = DeckCraft.InstallFolder();
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(folder)!);
            CopyFolder(unpacked, folder);
            progress.WriteLine($"Installed DeckCraft {version} in {folder}");
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PowerPointCompareForGit", "1.0"));
        return client;
    }

    private static string LatestVersion(HttpClient client)
    {
        var json = client.GetStringAsync("https://api.github.com/repos/storytold/deckcraft/releases/latest").GetAwaiter().GetResult();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("tag_name").GetString()!.TrimStart('v');
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
            progress.WriteLine($"Rendering will work, but the DeckCraft app wasn't installed ({ex.Message}). Get it with {brew.CommandText} or from {Releases}.");
        }
    }
}
