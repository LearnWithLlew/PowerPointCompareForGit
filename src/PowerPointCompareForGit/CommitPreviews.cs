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

        var soffice = FindLibreOffice();
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

    // Renders the deck as it is on disk. Never cached: it is re-rendered on every run.
    internal static string RenderWorkingCopy(GitRepository repository, string relativePath, string deckPath)
    {
        if (!File.Exists(deckPath))
        {
            throw new FileNotFoundException($"{deckPath} does not exist.");
        }

        var previewPath = PreviewPaths.For(repository.Root, relativePath, PreviewPaths.WorkingCopy);
        DeckRenderer.Render(FindLibreOffice(), deckPath, previewPath);
        return previewPath;
    }

    // version is a commit sha or PreviewPaths.WorkingCopy.
    internal static string Render(GitRepository repository, string relativePath, string deckPath, string version)
        => version == PreviewPaths.WorkingCopy
            ? RenderWorkingCopy(repository, relativePath, deckPath)
            : Render(repository, relativePath, version);

    // Prompts go to stderr so stdout stays a clean list of PNG paths.
    private static string FindLibreOffice()
    {
        var configuredPath = Settings.Load(Settings.DefaultPath()).LibreOfficePath;
        return LibreOfficeSetup.Resolve(
            () => LibreOffice.Find(configuredPath),
            LibreOfficeSetup.InstallerForThisOs(),
            Console.In,
            Console.Error,
            Console.IsInputRedirected,
            LibreOfficeSetup.RunInstaller);
    }
}
