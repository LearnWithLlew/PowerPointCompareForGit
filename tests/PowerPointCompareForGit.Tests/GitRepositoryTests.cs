namespace PowerPointCompareForGit.Tests;

public class GitRepositoryTests
{
    internal static string Git(string folder, params string[] arguments)
    {
        var result = ProcessRunner.Run("git", ["-c", "user.name=Test", "-c", "user.email=test@example.com", .. arguments], folder);
        Assert.True(result.ExitCode == 0, result.Error);
        return result.OutputText;
    }

    internal static TestFolder RepoWithDeck(out string deckPath)
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
    public void ExtractFileAppliesFiltersLikeGitLfs()
    {
        using var folder = RepoWithDeck(out var deckPath);
        File.WriteAllText(folder.Combine(".gitattributes"), "*.txt filter=fake\n");
        Git(folder.Path, "config", "filter.fake.smudge", "sed s/pointer/content/");
        Git(folder.Path, "config", "filter.fake.clean", "cat");
        File.WriteAllText(folder.Combine("stored.txt"), "pointer\n");
        Git(folder.Path, "add", ".");
        Git(folder.Path, "commit", "-q", "-m", "filtered file");
        var repository = GitRepository.Containing(deckPath);
        var extracted = folder.Combine("extracted.txt");

        repository.ExtractFile(repository.ResolveCommit("HEAD"), "stored.txt", extracted);

        Assert.Equal("content\n", File.ReadAllText(extracted));
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

    [Fact]
    public void ResolvesShortShaAndBranchNames()
    {
        using var folder = RepoWithDeck(out var deckPath);
        var head = Git(folder.Path, "rev-parse", "HEAD");
        var repository = GitRepository.Containing(deckPath);

        Assert.Equal(head, repository.ResolveCommit(head[..7]));
        Assert.Equal(head, repository.ResolveCommit("HEAD"));
    }

    [Fact]
    public void UnknownCommitFails()
    {
        using var folder = RepoWithDeck(out var deckPath);

        var error = Assert.Throws<InvalidOperationException>(() => GitRepository.Containing(deckPath).ResolveCommit("nope"));
        Assert.Equal("nope is not a commit in this repository.", error.Message);
    }

    [Fact]
    public void FileExistsAtCommit()
    {
        using var folder = RepoWithDeck(out var deckPath);
        var repository = GitRepository.Containing(deckPath);
        var head = repository.ResolveCommit("HEAD");

        Assert.True(repository.FileExistsAt(head, "decks/my.pptx"));
        Assert.False(repository.FileExistsAt(head, "decks/other.pptx"));
    }

    [Fact]
    public void MissingFolderFails()
    {
        using var folder = new TestFolder();

        Assert.Throws<DirectoryNotFoundException>(() => GitRepository.Containing(folder.Combine("missing", "my.pptx")));
    }
}
