namespace PowerPointCompareForGit.Tests;

public class DeckInRepoTests
{
    [Fact]
    public void RelativeDeckPathBecomesAbsolute()
    {
        using var folder = GitRepositoryTests.RepoWithDeck(out var deckPath);
        var relativeToHere = Path.GetRelativePath(Environment.CurrentDirectory, deckPath);

        var deck = DeckInRepo.At(relativeToHere);

        Assert.Equal(Path.GetFullPath(deckPath), deck.WorkingCopyPath);
    }
}
