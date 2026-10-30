namespace PowerPointCompareForGit;

internal static class ComparePlan
{
    // Picks the two versions --compare opens, older on the left. A version is a sha or PreviewPaths.WorkingCopy.
    // No shas: working copy vs last commit that changed the file, or the last two such commits if nothing is uncommitted.
    // One sha: working copy vs it. Two shas: the first vs the second.
    internal static (string Left, string Right) Choose(GitRepository repository, string relativePath, IReadOnlyList<string> shas)
    {
        switch (shas.Count)
        {
            case 2:
                return (repository.ResolveCommit(shas[0]), repository.ResolveCommit(shas[1]));
            case 1:
                return (repository.ResolveCommit(shas[0]), PreviewPaths.WorkingCopy);
        }

        if (repository.HasUncommittedChanges(relativePath))
        {
            var last = repository.LastCommitThatChanged(relativePath)
                ?? throw new InvalidOperationException($"{relativePath} has not been committed yet, so there is nothing to compare it with.");
            return (last, PreviewPaths.WorkingCopy);
        }

        var commits = repository.LastCommitsThatChanged(relativePath, 2);
        return commits.Count == 2
            ? (commits[1], commits[0])
            : throw new InvalidOperationException($"{relativePath} has only one version, so there is nothing to compare it with.");
    }
}
