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
    public void PageFileSitsNextToTheOtherPreviews()
    {
        var path = PreviewPaths.ForPage(RepoRoot, "decks/my.pptx", "0123456789abcdef", 1);

        Assert.Equal(Expected("decks", "my.pptx.0123456.page.1.png"), path);
    }

    [Fact]
    public void StitchedRangeFileNamesTheRange()
    {
        var path = PreviewPaths.ForPages(RepoRoot, "decks/my.pptx", PreviewPaths.WorkingCopy, new PageRange(2, 25));

        Assert.Equal(Expected("decks", "my.pptx.working.pages.2-25.png"), path);
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

    // Known gaps: these pass today and document wrong behavior. Flip them when fixed (see TODO in Project.md).

    [Fact]
    public void KnownGap_FolderStartingWithDotDotIsRejected()
    {
        // "..decks" is a normal folder inside the repo, but the check only looks for a leading "..".
        Assert.Throws<ArgumentException>(() => PreviewPaths.For(RepoRoot, "..decks/my.pptx", "0123456789abcdef"));
    }

    [Fact]
    public void KnownGap_WorktreeGitFileIsTreatedAsFolder()
    {
        // In a git worktree or submodule, .git is a file pointing at the real git dir, so .git/pptx-previews can't be created.
        var path = PreviewPaths.For(RepoRoot, "my.pptx", "0123456789abcdef");

        Assert.StartsWith(Path.Combine(RepoRoot, ".git", "pptx-previews"), path);
    }
}
