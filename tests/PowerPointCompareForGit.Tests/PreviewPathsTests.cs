namespace PowerPointCompareForGit.Tests;

public class PreviewPathsTests
{
    private static readonly string RepoRoot = Path.Combine(Path.GetTempPath(), "repo");
    private static readonly PreviewPaths MyDeck = new(RepoRoot, "decks/my.pptx");

    private static string Expected(params string[] parts)
        => Path.Combine([RepoRoot, ".git", "pptx-previews", .. parts]);

    [Fact]
    public void ShortensShaToSevenCharacters()
    {
        var path = MyDeck.For("0123456789abcdef");

        Assert.Equal(Expected("decks", "my.pptx.0123456.png"), path);
    }

    [Fact]
    public void SelectiveNamesBothVersions()
    {
        var path = MyDeck.ForSelective("0123456789abcdef", "fedcba9876543210");

        Assert.Equal(Expected("decks", "my.pptx.0123456.selective-vs.fedcba9.png"), path);
    }

    [Fact]
    public void SelectiveNamesTheWorkingCopyAndRenderer()
    {
        var path = new PreviewPaths(RepoRoot, "decks/my.pptx", Renderer.DeckCraft).ForSelective(PreviewPaths.WorkingCopy, "fedcba9876543210");

        Assert.Equal(Expected("decks", "my.pptx.working.deckcraft.selective-vs.fedcba9.png"), path);
    }

    [Fact]
    public void WorkingCopyUsesWorkingLabel()
    {
        var path = MyDeck.For(PreviewPaths.WorkingCopy);

        Assert.Equal(Expected("decks", "my.pptx.working.png"), path);
    }

    [Fact]
    public void PageFileSitsNextToTheOtherPreviews()
    {
        var path = MyDeck.ForPage("0123456789abcdef", 1);

        Assert.Equal(Expected("decks", "my.pptx.0123456.page.1.png"), path);
    }

    [Fact]
    public void StitchedRangeFileNamesTheRange()
    {
        var path = MyDeck.ForPages(PreviewPaths.WorkingCopy, new PageRange(2, 25));

        Assert.Equal(Expected("decks", "my.pptx.working.pages.2-25.png"), path);
    }

    [Fact]
    public void OtherRenderersGetTheirOwnFiles()
    {
        Assert.Equal(Expected("decks", "my.pptx.0123456.deckcraft.png"), new PreviewPaths(RepoRoot, "decks/my.pptx", Renderer.DeckCraft).For("0123456789abcdef"));
        Assert.Equal(Expected("decks", "my.pptx.working.deckcraft.page.1.png"), new PreviewPaths(RepoRoot, "decks/my.pptx", Renderer.DeckCraft).ForPage(PreviewPaths.WorkingCopy, 1));
        Assert.Equal(Expected("decks", "my.pptx.0123456.deckcraft.pages.2-25.png"), new PreviewPaths(RepoRoot, "decks/my.pptx", Renderer.DeckCraft).ForPages("0123456789abcdef", new PageRange(2, 25)));
    }

    [Fact]
    public void DeckAtRepoRootHasNoSubfolder()
    {
        var path = new PreviewPaths(RepoRoot, "my.pptx").For("0123456789abcdef");

        Assert.Equal(Expected("my.pptx.0123456.png"), path);
    }

    [Fact]
    public void AcceptsAbsoluteDeckPath()
    {
        var path = new PreviewPaths(RepoRoot, Path.Combine(RepoRoot, "decks", "my.pptx")).For("0123456789abcdef");

        Assert.Equal(Expected("decks", "my.pptx.0123456.png"), path);
    }

    [Fact]
    public void RejectsDeckOutsideRepo()
    {
        Assert.Throws<ArgumentException>(() => new PreviewPaths(RepoRoot, "../other/my.pptx").For("0123456789abcdef"));
    }

    // Known gaps: these pass today and document wrong behavior. Flip them when fixed (see TODO in Project.md).

    [Fact]
    public void KnownGap_FolderStartingWithDotDotIsRejected()
    {
        // "..decks" is a normal folder inside the repo, but the check only looks for a leading "..".
        Assert.Throws<ArgumentException>(() => new PreviewPaths(RepoRoot, "..decks/my.pptx").For("0123456789abcdef"));
    }

    [Fact]
    public void KnownGap_WorktreeGitFileIsTreatedAsFolder()
    {
        // In a git worktree or submodule, .git is a file pointing at the real git dir, so .git/pptx-previews can't be created.
        var path = new PreviewPaths(RepoRoot, "my.pptx").For("0123456789abcdef");

        Assert.StartsWith(Path.Combine(RepoRoot, ".git", "pptx-previews"), path);
    }

    [Fact]
    public void ClearDeletesTheWholeCache()
    {
        using var folder = new TestFolder();
        var preview = new PreviewPaths(folder.Path, "decks/my.pptx").For("0123456789abcdef");
        Directory.CreateDirectory(Path.GetDirectoryName(preview)!);
        File.WriteAllText(preview, "");

        Assert.True(PreviewPaths.Clear(folder.Path));
        Assert.False(Directory.Exists(PreviewPaths.CacheFolder(folder.Path)));
        Assert.False(PreviewPaths.Clear(folder.Path));
    }

    [Fact]
    public void ClearDeckCraftKeepsLibreOfficePreviews()
    {
        using var folder = new TestFolder();
        string[] deckCraft =
        [
            new PreviewPaths(folder.Path, "decks/my.pptx", Renderer.DeckCraft).For("0123456789abcdef"),
            new PreviewPaths(folder.Path, "decks/my.pptx", Renderer.DeckCraft).ForPage(PreviewPaths.WorkingCopy, 2)
        ];
        var libreOffice = new PreviewPaths(folder.Path, "decks/my.pptx").For("0123456789abcdef");
        foreach (var preview in deckCraft.Append(libreOffice))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(preview)!);
            File.WriteAllText(preview, "");
        }

        PreviewPaths.ClearDeckCraft(folder.Path);

        Assert.All(deckCraft, preview => Assert.False(File.Exists(preview)));
        Assert.True(File.Exists(libreOffice));
    }
}
