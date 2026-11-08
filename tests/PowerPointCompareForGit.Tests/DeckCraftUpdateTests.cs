namespace PowerPointCompareForGit.Tests;

public class DeckCraftUpdateTests
{
    [Fact]
    public void UpdateCheckIsDueOncePerDay()
    {
        using var folder = new TestFolder();
        var stamp = folder.Combine("deckcraft-update-check.txt");
        Assert.True(DeckCraftUpdate.UpdateCheckIsDue(stamp, DateTime.UtcNow));

        File.WriteAllText(stamp, "");
        var checkedAt = File.GetLastWriteTimeUtc(stamp);

        Assert.False(DeckCraftUpdate.UpdateCheckIsDue(stamp, checkedAt.AddHours(23)));
        Assert.True(DeckCraftUpdate.UpdateCheckIsDue(stamp, checkedAt.AddDays(1)));
    }

    [Fact]
    public void ReadsTheInstalledVersion()
    {
        Assert.Equal(new Version(0, 13, 1), DeckCraftUpdate.InstalledVersion("deckcraft-cli 0.13.1\n"));
        Assert.Null(DeckCraftUpdate.InstalledVersion("unexpected"));
    }

    [Fact]
    public void SameOrOlderVersionIsNotOffered()
    {
        var prompt = new StringWriter();

        Assert.False(DeckCraftUpdate.OfferUpdate(new Version(0, 16, 0), "0.16.0", new UserConsole(new StringReader("1\n"), prompt, false)));
        Assert.False(DeckCraftUpdate.OfferUpdate(new Version(0, 16, 0), "0.15.1", new UserConsole(new StringReader("1\n"), prompt, false)));
        Assert.False(DeckCraftUpdate.OfferUpdate(new Version(0, 16, 0), "not-a-version", new UserConsole(new StringReader("1\n"), prompt, false)));
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void NewerVersionIsOffered()
    {
        var prompt = new StringWriter();

        Assert.True(DeckCraftUpdate.OfferUpdate(new Version(0, 13, 1), "0.16.0", new UserConsole(new StringReader("1\n"), prompt, false)));
        Assert.StartsWith("DeckCraft 0.16.0 is available (you have 0.13.1).", prompt.ToString());
        Assert.False(DeckCraftUpdate.OfferUpdate(new Version(0, 13, 1), "0.16.0", new UserConsole(new StringReader("2\n"), new StringWriter(), false)));
    }

    [Fact]
    public void PipedInputOnlyNotesTheUpdate()
    {
        var prompt = new StringWriter();

        Assert.False(DeckCraftUpdate.OfferUpdate(new Version(0, 13, 1), "0.16.0", new UserConsole(new StringReader("1\n"), prompt, true)));
        Assert.Contains("Run CreatePowerpointCheckpoint in a terminal to update it.", prompt.ToString());
        Assert.DoesNotContain("Choice:", prompt.ToString());
    }
}
