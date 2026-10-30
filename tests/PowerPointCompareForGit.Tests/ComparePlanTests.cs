namespace PowerPointCompareForGit.Tests;

public class ComparePlanTests
{
    private const string Deck = "decks/my.pptx";

    private static string CommitEdit(TestFolder folder, string deckPath, string content)
    {
        File.WriteAllText(deckPath, content);
        GitRepositoryTests.Git(folder.Path, "commit", "-q", "-am", content);
        return GitRepositoryTests.Git(folder.Path, "rev-parse", "HEAD");
    }

    [Fact]
    public void NoShasAndNoEditsComparesLastTwoCommits()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var first = GitRepositoryTests.Git(folder.Path, "rev-parse", "HEAD");
        var second = CommitEdit(folder, deckPath, "second");

        Assert.Equal((first, second), ComparePlan.Choose(GitRepository.Containing(deckPath), Deck, []));
    }

    [Fact]
    public void NoShasWithEditsComparesLastCommitToWorkingCopy()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var last = GitRepositoryTests.Git(folder.Path, "rev-parse", "HEAD");
        File.WriteAllText(deckPath, "edited");

        Assert.Equal((last, PreviewPaths.WorkingCopy), ComparePlan.Choose(GitRepository.Containing(deckPath), Deck, []));
    }

    [Fact]
    public void OneShaComparesItToWorkingCopy()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var first = GitRepositoryTests.Git(folder.Path, "rev-parse", "HEAD");
        CommitEdit(folder, deckPath, "second");

        Assert.Equal((first, PreviewPaths.WorkingCopy), ComparePlan.Choose(GitRepository.Containing(deckPath), Deck, [first[..7]]));
    }

    [Fact]
    public void TwoShasCompareInTheGivenOrder()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var first = GitRepositoryTests.Git(folder.Path, "rev-parse", "HEAD");
        var second = CommitEdit(folder, deckPath, "second");

        Assert.Equal((second, first), ComparePlan.Choose(GitRepository.Containing(deckPath), Deck, [second, first]));
    }

    [Fact]
    public void SingleUneditedVersionHasNothingToCompare()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);

        var error = Assert.Throws<InvalidOperationException>(() => ComparePlan.Choose(GitRepository.Containing(deckPath), Deck, []));
        Assert.Contains("only one version", error.Message);
    }

    [Fact]
    public void NeverCommittedDeckHasNothingToCompare()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        File.Copy(deckPath, folder.Combine("decks", "new.pptx"));

        var error = Assert.Throws<InvalidOperationException>(
            () => ComparePlan.Choose(GitRepository.Containing(deckPath), "decks/new.pptx", []));
        Assert.Contains("has not been committed yet", error.Message);
    }
}
