namespace PowerPointCompareForGit.Tests;

public class GitRepositoryTests
{
    private static string Git(string folder, params string[] arguments)
    {
        var result = ProcessRunner.Run("git", ["-c", "user.name=Test", "-c", "user.email=test@example.com", .. arguments], folder);
        Assert.True(result.ExitCode == 0, result.Error);
        return result.OutputText;
    }

    private static TestFolder RepoWithDeck(out string deckPath)
    {
        var folder = new TestFolder();
        Directory.CreateDirectory(folder.Combine("decks"));
        deckPath = folder.Combine("decks", "my.pptx");
        File.Copy(TestFolder.ThreeSlideDeck, deckPath);
        Git(folder.Path, "init", "-q");
        Git(folder.Path, "add", ".");
        Git(folder.Path, "commit", "-q", "-m", "add deck");
        return folder;
    }

    [Fact]
    public void FindsRelativePathFromNestedFolder()
    {
        using var folder = RepoWithDeck(out var deckPath);

        Assert.Equal("decks/my.pptx", GitRepository.Containing(deckPath).RelativePath(deckPath));
    }

    [Fact]
    public void LastCommitThatChangedIgnoresLaterUnrelatedCommits()
    {
        using var folder = RepoWithDeck(out var deckPath);
        var deckCommit = Git(folder.Path, "rev-parse", "HEAD");
        File.WriteAllText(folder.Combine("other.txt"), "unrelated");
        Git(folder.Path, "add", ".");
        Git(folder.Path, "commit", "-q", "-m", "unrelated");

        Assert.Equal(deckCommit, GitRepository.Containing(deckPath).LastCommitThatChanged("decks/my.pptx"));
    }

    [Fact]
    public void UncommittedFileHasNoLastCommit()
    {
        using var folder = RepoWithDeck(out var deckPath);
        File.Copy(deckPath, folder.Combine("decks", "new.pptx"));

        Assert.Null(GitRepository.Containing(deckPath).LastCommitThatChanged("decks/new.pptx"));
    }

    [Fact]
    public void ExtractsFileFromCommit()
    {
        using var folder = RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var sha = repository.LastCommitThatChanged("decks/my.pptx")!;
        File.WriteAllText(deckPath, "edited");
        var extracted = folder.Combine("extracted.pptx");

        repository.ExtractFile(sha, "decks/my.pptx", extracted);

        Assert.Equal(File.ReadAllBytes(TestFolder.ThreeSlideDeck), File.ReadAllBytes(extracted));
    }

    [Fact]
    public void OutsideRepoFails()
    {
        using var folder = new TestFolder();

        Assert.Throws<InvalidOperationException>(() => GitRepository.Containing(folder.Combine("my.pptx")));
    }
}
