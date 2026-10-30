namespace PowerPointCompareForGit;

public static class Program
{
    public static int Main(string[] args)
    {
        var rootCommand = CommandLineArguments.CreateRootCommand();
        rootCommand.SetAction(parseResult =>
        {
            var request = CommandLineArguments.ToRequest(parseResult);
            try
            {
                if (request.Reset || request.Compare || request.Commits.Count > 0)
                {
                    Console.Error.WriteLine("Not implemented yet: --reset, --compare and --commit.");
                    return 1;
                }

                Console.WriteLine(RenderLastCommit(request.File!));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return rootCommand.Parse(args).Invoke();
    }

    // Renders the last commit that changed the deck (or reuses the cached preview) and returns the PNG path.
    private static string RenderLastCommit(string deckPath)
    {
        if (!File.Exists(deckPath))
        {
            throw new FileNotFoundException($"{deckPath} does not exist.");
        }

        var repository = GitRepository.Containing(deckPath);
        var relativePath = repository.RelativePath(deckPath);
        var sha = repository.LastCommitThatChanged(relativePath)
            ?? throw new InvalidOperationException($"{relativePath} has not been committed yet.");

        var previewPath = PreviewPaths.For(repository.Root, relativePath, sha);
        if (File.Exists(previewPath))
        {
            return previewPath;
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
