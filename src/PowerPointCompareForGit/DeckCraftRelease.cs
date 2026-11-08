using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PowerPointCompareForGit;

internal static class DeckCraftRelease
{
    internal const string Releases = "https://github.com/storytold/deckcraft/releases";

    internal sealed record Asset(string Version, string Name, string Url, string ExpectedHash);

    internal static Asset Latest(HttpClient client, OSPlatform os)
    {
        var version = LatestVersion(client);
        var name = AssetName(version, os, RuntimeInformation.OSArchitecture);
        var download = $"{Releases}/download/v{version}";
        var checksums = client.GetStringAsync($"{download}/SHA256SUMS.txt").GetAwaiter().GetResult();
        var expected = ExpectedHash(checksums, name) ?? throw new InvalidOperationException($"SHA256SUMS.txt has no entry for {name}, so it was not installed.");
        return new Asset(version, name, $"{download}/{name}", expected);
    }

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

    internal static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PowerPointCompareForGit", "1.0"));
        return client;
    }

    internal static string LatestVersion(HttpClient client)
    {
        var json = client.GetStringAsync("https://api.github.com/repos/storytold/deckcraft/releases/latest").GetAwaiter().GetResult();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("tag_name").GetString()!.TrimStart('v');
    }
}
