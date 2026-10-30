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
                if (request.Reset || request.Compare)
                {
                    Console.Error.WriteLine("Not implemented yet: --reset and --compare.");
                    return 1;
                }

                foreach (var previewPath in RenderRequestedCommits(request))
                {
                    Console.WriteLine(previewPath);
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
