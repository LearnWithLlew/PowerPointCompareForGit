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
                if (request.Reset)
                {
                    Reset(Settings.DefaultPath());
                }
                else if (request.Compare)
                {
                    Compare(request);
                }
                else
                {
                    foreach (var previewPath in RenderRequestedCommits(request))
                    {
                        Console.WriteLine(previewPath);
                    }
                }

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

    private static void Reset(string settingsPath)
    {
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
            Console.WriteLine($"Deleted {settingsPath}");
        }
        else
        {
            Console.WriteLine($"No settings to delete ({settingsPath} does not exist).");
        }
    }

    // Renders both versions, prints their paths (older first) and opens them in the diff tool.
    private static void Compare(Request request)
    {
        var deckPath = request.File!;
        var repository = GitRepository.Containing(deckPath);
        var relativePath = repository.RelativePath(deckPath);
        var (left, right) = ComparePlan.Choose(repository, relativePath, request.CompareShas);

        var leftPreview = CommitPreviews.Render(repository, relativePath, deckPath, left);
        var rightPreview = CommitPreviews.Render(repository, relativePath, deckPath, right);
        Console.WriteLine(leftPreview);
        Console.WriteLine(rightPreview);

        var diffTool = DiffToolSetup.Resolve(Settings.DefaultPath(), DiffTools.Detect(), Console.In, Console.Out, Console.IsInputRedirected);
        DiffTools.Launch(diffTool, leftPreview, rightPreview);
    }

    // --commit renders each listed commit; with no flags, the last commit that changed the deck.
    private static IEnumerable<string> RenderRequestedCommits(Request request)
    {
        var deckPath = request.File!;
        var repository = GitRepository.Containing(deckPath);
        var relativePath = repository.RelativePath(deckPath);

        if (request.Commits.Count > 0)
        {
            // Resolve every commit before rendering any, so a typo fails fast.
            var commits = request.Commits.Select(repository.ResolveCommit).ToList();
            foreach (var commit in commits)
            {
                yield return CommitPreviews.Render(repository, relativePath, commit);
            }

            yield break;
        }

        var sha = repository.LastCommitThatChanged(relativePath)
            ?? throw new InvalidOperationException($"{relativePath} has not been committed yet.");
        yield return CommitPreviews.Render(repository, relativePath, sha);
    }
}
