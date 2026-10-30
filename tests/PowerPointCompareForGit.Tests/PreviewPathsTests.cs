namespace PowerPointCompareForGit.Tests;

public class PreviewPathsTests
{
    private static readonly string RepoRoot = Path.Combine(Path.GetTempPath(), "repo");

    private static string Expected(params string[] parts)
        => Path.Combine([RepoRoot, ".git", "pptx-previews", .. parts]);

    [Fact]
    public void ShortensShaToSevenCharacters()
    {
        var path = PreviewPaths.For(RepoRoot, "decks/my.pptx", "0123456789abcdef");

        Assert.Equal(Expected("decks", "my.pptx.0123456.png"), path);
    }

    [Fact]
    public void WorkingCopyUsesWorkingLabel()
    {
        var path = PreviewPaths.For(RepoRoot, "decks/my.pptx", PreviewPaths.WorkingCopy);

        Assert.Equal(Expected("decks", "my.pptx.working.png"), path);
    }

    [Fact]
    public void DeckAtRepoRootHasNoSubfolder()
    {
        var path = PreviewPaths.For(RepoRoot, "my.pptx", "0123456789abcdef");

        Assert.Equal(Expected("my.pptx.0123456.png"), path);
    }

    [Fact]
    public void AcceptsAbsoluteDeckPath()
    {
        var path = PreviewPaths.For(RepoRoot, Path.Combine(RepoRoot, "decks", "my.pptx"), "0123456789abcdef");

        Assert.Equal(Expected("decks", "my.pptx.0123456.png"), path);
    }

    [Fact]
    public void RejectsDeckOutsideRepo()
    {
        Assert.Throws<ArgumentException>(() => PreviewPaths.For(RepoRoot, "../other/my.pptx", "0123456789abcdef"));
    }
}
