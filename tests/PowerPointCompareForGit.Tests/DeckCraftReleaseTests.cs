using System.Runtime.InteropServices;

namespace PowerPointCompareForGit.Tests;

public class DeckCraftReleaseTests
{
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
        Assert.Equal(expected, DeckCraftRelease.AssetName("1.2.3", os, architecture));
    }

    [Fact]
    public void UnsupportedArchitectureFails()
    {
        Assert.Throws<PlatformNotSupportedException>(() => DeckCraftRelease.AssetName("1.2.3", OSPlatform.Linux, Architecture.S390x));
    }

    [Fact]
    public void FindsHashForTheExactFile()
    {
        const string checksums = """
            aaa  deckcraft-cli-1.2.3-macos-universal.zip
            bbb  deckcraft-cli-1.2.3-macos-universal.zip.sig
            ccc *deckcraft-1.2.3-linux-x86_64.tar.gz
            """;

        Assert.Equal("aaa", DeckCraftRelease.ExpectedHash(checksums, "deckcraft-cli-1.2.3-macos-universal.zip"));
        Assert.Equal("ccc", DeckCraftRelease.ExpectedHash(checksums, "deckcraft-1.2.3-linux-x86_64.tar.gz"));
        Assert.Null(DeckCraftRelease.ExpectedHash(checksums, "other.zip"));
    }
}
