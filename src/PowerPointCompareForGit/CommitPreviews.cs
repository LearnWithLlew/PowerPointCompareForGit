namespace PowerPointCompareForGit;

internal static class CommitPreviews
{
    // Returns the preview PNG for the deck as of the commit, rendering it only if it isn't cached yet.
    internal static string Render(GitRepository repository, string relativePath, string sha)
    {
        var previewPath = PreviewPaths.For(repository.Root, relativePath, sha);
        if (File.Exists(previewPath))
        {
            return previewPath;
        }

        if (!repository.FileExistsAt(sha, relativePath))
        {
            throw new InvalidOperationException($"{relativePath} does not exist in commit {sha[..7]}.");
        }

        var soffice = LibreOffice.Find() ?? throw new InvalidOperationException(LibreOffice.NotFoundMessage());
        var work = Directory.CreateTempSubdirectory("pptx-extract-");
        try
        {
            var deckCopy = Path.Combine(work.FullName, Path.GetFileName(relativePath));
            repository.ExtractFile(sha, relativePath, deckCopy);
            DeckRenderer.Render(soffice, deckCopy, previewPath);
        }
        finally
        {
            work.Delete(recursive: true);
        }

        return previewPath;
    }
}
