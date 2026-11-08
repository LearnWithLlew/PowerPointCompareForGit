namespace PowerPointCompareForGit;

internal sealed record DeckInRepo(GitRepository Repository, string RelativePath, string WorkingCopyPath)
{
    internal static DeckInRepo At(string deckPath)
        => new(GitRepository.Containing(deckPath), GitRepository.RelativePath(deckPath), deckPath);

    // Runs action on the deck as of the version: the file on disk for the working copy, otherwise a temporary copy
    // from git. Which pages exist depends on the deck, so this is needed even when the previews are already cached.
    internal T AsOf<T>(string version, Func<string, T> action)
    {
        if (version == PreviewPaths.WorkingCopy)
        {
            return File.Exists(WorkingCopyPath) ? action(WorkingCopyPath) : throw new FileNotFoundException($"{WorkingCopyPath} does not exist.");
        }

        if (!Repository.FileExistsAt(version, RelativePath))
        {
            throw new InvalidOperationException($"{RelativePath} does not exist in commit {version[..7]}.");
        }

        var work = Directory.CreateTempSubdirectory("pptx-extract-");
        try
        {
            var deckCopy = Path.Combine(work.FullName, Path.GetFileName(RelativePath));
            Repository.ExtractFile(version, RelativePath, deckCopy);
            return action(deckCopy);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }
}
