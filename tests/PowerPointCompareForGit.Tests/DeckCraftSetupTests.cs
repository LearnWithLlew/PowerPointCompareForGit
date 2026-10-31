using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PowerPointCompareForGit.Tests;

public class DeckCraftSetupTests
{
    [Fact]
    public void FoundNeedsNoPrompt()
    {
        var prompt = new StringWriter();

        var cli = DeckCraftSetup.Resolve(() => "/bin/deckcraft-cli", new StringReader(""), prompt, false, () => Assert.Fail("Should not install"));

        Assert.Equal("/bin/deckcraft-cli", cli);
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void ChoosingInstallRunsInstallerAndFindsAgain()
    {
        string? installed = null;

        var cli = DeckCraftSetup.Resolve(() => installed, new StringReader("1\n"), new StringWriter(), false, () => installed = "/new/deckcraft-cli");

        Assert.Equal("/new/deckcraft-cli", cli);
    }

    [Fact]
    public void PromptOffersBothAndTheAlternatives()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(() => DeckCraftSetup.Resolve(() => null, new StringReader("2\n"), prompt, false, () => { }));

        Assert.Contains("1. Install DeckCraft and deckcraft-cli", prompt.ToString());
        Assert.Contains("--renderer LibreOffice", prompt.ToString());
    }

    [Fact]
    public void CancelDoesNotInstall()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new StringReader("2\n"), new StringWriter(), false, () => Assert.Fail("Should not install")));

        Assert.StartsWith("Cancelled.", error.Message);
    }

    [Fact]
    public void PipedInputFailsWithoutPrompting()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new StringReader("1\n"), prompt, true, () => Assert.Fail("Should not install")));
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void InstallThatFindsNothingFails()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new StringReader("1\n"), new StringWriter(), false, () => { }));

        Assert.StartsWith("deckcraft-cli still wasn't found", error.Message);
    }

    public static TheoryData<OSPlatform, Architecture, string> Assets => new()
    {
        { OSPlatform.OSX, Architecture.Arm64, "deckcraft-cli-1.2.3-macos-universal.zip" },
        { OSPlatform.OSX, Architecture.X64, "deckcraft-cli-1.2.3-macos-universal.zip" },
        { OSPlatform.Windows, Architecture.X64, "deckcraft-1.2.3-windows-x64-portable.zip" },
        { OSPlatform.Windows, Architecture.Arm64, "deckcraft-1.2.3-windows-arm64-portable.zip" },
        { OSPlatform.Windows, Architecture.X86, "deckcraft-1.2.3-windows-x86-portable.zip" },
        { OSPlatform.Linux, Architecture.X64, "deckcraft-1.2.3-linux-x86_64.tar.gz" },
        { OSPlatform.Linux, Architecture.Arm64, "deckcraft-1.2.3-linux-aarch64.tar.gz" },
        { OSPlatform.Linux, Architecture.RiscV64, "deckcraft-1.2.3-linux-riscv64.tar.gz" }
    };

    [Theory]
    [MemberData(nameof(Assets))]
    public void PicksTheReleaseFile(OSPlatform os, Architecture architecture, string expected)
    {
        Assert.Equal(expected, DeckCraftSetup.AssetName("1.2.3", os, architecture));
    }

    [Fact]
    public void UnsupportedArchitectureFails()
    {
        Assert.Throws<PlatformNotSupportedException>(() => DeckCraftSetup.AssetName("1.2.3", OSPlatform.Linux, Architecture.S390x));
    }

    [Fact]
    public void FindsHashForTheExactFile()
    {
        const string checksums = """
            aaa  deckcraft-cli-1.2.3-macos-universal.zip
            bbb  deckcraft-cli-1.2.3-macos-universal.zip.sig
            ccc *deckcraft-1.2.3-linux-x86_64.tar.gz
            """;

        Assert.Equal("aaa", DeckCraftSetup.ExpectedHash(checksums, "deckcraft-cli-1.2.3-macos-universal.zip"));
        Assert.Equal("ccc", DeckCraftSetup.ExpectedHash(checksums, "deckcraft-1.2.3-linux-x86_64.tar.gz"));
        Assert.Null(DeckCraftSetup.ExpectedHash(checksums, "other.zip"));
    }

    [Fact]
    public void VerifyHashAcceptsMatchAndRejectsMismatch()
    {
        using var folder = new TestFolder();
        var file = folder.Combine("download.zip");
        File.WriteAllText(file, "content");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();

        DeckCraftSetup.VerifyHash(file, hash);

        var error = Assert.Throws<InvalidOperationException>(() => DeckCraftSetup.VerifyHash(file, new string('0', 64)));
        Assert.Equal("download.zip failed its checksum, so it was not installed.", error.Message);
    }

    [Fact]
    public void UnpacksZip()
    {
        using var folder = new TestFolder();
        var source = Directory.CreateDirectory(folder.Combine("source", "inner")).FullName;
        File.WriteAllText(Path.Combine(source, "deckcraft-cli"), "cli");
        var archive = folder.Combine("archive.zip");
        ZipFile.CreateFromDirectory(folder.Combine("source"), archive);

        DeckCraftSetup.Unpack(archive, folder.Combine("out"));

        Assert.Equal("cli", File.ReadAllText(folder.Combine("out", "inner", "deckcraft-cli")));
    }

    [Fact]
    public void UnpacksTarGz()
    {
        using var folder = new TestFolder();
        var source = Directory.CreateDirectory(folder.Combine("source", "bin")).FullName;
        File.WriteAllText(Path.Combine(source, "deckcraft-cli"), "cli");
        var archive = folder.Combine("archive.tar.gz");
        using (var file = File.Create(archive))
        using (var gzip = new GZipStream(file, CompressionMode.Compress))
        {
            TarFile.CreateFromDirectory(folder.Combine("source"), gzip, includeBaseDirectory: false);
        }

        DeckCraftSetup.Unpack(archive, folder.Combine("out"));

        Assert.Equal("cli", File.ReadAllText(folder.Combine("out", "bin", "deckcraft-cli")));
    }

    [Fact]
    public void FindsTheCliAnywhereInTheInstallFolder()
    {
        using var folder = new TestFolder();
        var name = OperatingSystem.IsWindows() ? "deckcraft-cli.exe" : "deckcraft-cli";
        var bin = Directory.CreateDirectory(folder.Combine("deckcraft", "deckcraft-1.2.3", "bin")).FullName;
        var cli = Path.Combine(bin, name);
        File.WriteAllText(cli, "cli");

        Assert.Equal(cli, DeckCraft.FindIn(folder.Combine("deckcraft")));
        Assert.Null(DeckCraft.FindIn(folder.Combine("missing")));
    }
}
