using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;

namespace PowerPointCompareForGit.Tests;

public class DeckCraftSetupTests
{
    [Fact]
    public void FoundNeedsNoPrompt()
    {
        var prompt = new StringWriter();

        var cli = DeckCraftSetup.Resolve(() => "/bin/deckcraft-cli", new UserConsole(new StringReader(""), prompt, false), () => Assert.Fail("Should not install"));

        Assert.Equal("/bin/deckcraft-cli", cli);
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void ChoosingInstallRunsInstallerAndFindsAgain()
    {
        string? installed = null;

        var cli = DeckCraftSetup.Resolve(() => installed, new UserConsole(new StringReader("1\n"), new StringWriter(), false), () => installed = "/new/deckcraft-cli");

        Assert.Equal("/new/deckcraft-cli", cli);
    }

    [Fact]
    public void PromptOffersBothAndTheAlternatives()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(() => DeckCraftSetup.Resolve(() => null, new UserConsole(new StringReader("2\n"), prompt, false), () => { }));

        Assert.Contains("1. Install DeckCraft and deckcraft-cli", prompt.ToString());
        Assert.Contains("--renderer LibreOffice", prompt.ToString());
    }

    [Fact]
    public void CancelDoesNotInstall()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new UserConsole(new StringReader("2\n"), new StringWriter(), false), () => Assert.Fail("Should not install")));

        Assert.StartsWith("Cancelled.", error.Message);
    }

    [Fact]
    public void PipedInputFailsWithoutPrompting()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new UserConsole(new StringReader("1\n"), prompt, true), () => Assert.Fail("Should not install")));
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void InstallThatFindsNothingFails()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DeckCraftSetup.Resolve(() => null, new UserConsole(new StringReader("1\n"), new StringWriter(), false), () => { }));

        Assert.StartsWith("deckcraft-cli still wasn't found", error.Message);
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
