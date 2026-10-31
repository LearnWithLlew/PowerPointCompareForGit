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
        if (request.Selective)
        {
            CompareSelective(repository, relativePath, deckPath, left, right, request.Renderer);
            return;
        }

        string RenderVersion(string version) => request.Pages is { } pages
            ? CommitPreviews.RenderForCompare(repository, relativePath, deckPath, version, pages, request.Renderer)
            : CommitPreviews.Render(repository, relativePath, deckPath, version, request.Renderer);

        var leftPreview = RenderVersion(left);
        var rightPreview = RenderVersion(right);
        Console.WriteLine(leftPreview);
        Console.WriteLine(rightPreview);

        OpenInDiffTool(leftPreview, rightPreview);
    }

    // Opens only the slides that changed. Anything the images can't show goes to stderr, so stdout stays a clean list of PNG paths.
    private static void CompareSelective(GitRepository repository, string relativePath, string deckPath, string left, string right, Renderer renderer)
    {
        var previews = CommitPreviews.RenderSelective(repository, relativePath, deckPath, left, right, renderer);
        foreach (var message in previews.Alignment.Messages(VersionName(left), VersionName(right)))
        {
            Console.Error.WriteLine(message);
        }

        if (previews.Images is { } images)
        {
            Console.WriteLine(images.Left);
            Console.WriteLine(images.Right);
            OpenInDiffTool(images.Left, images.Right);
        }
    }

    private static string VersionName(string version)
        => version == PreviewPaths.WorkingCopy ? "the working copy" : version[..7];

    private static void OpenInDiffTool(string leftPreview, string rightPreview)
    {
        var diffTool = DiffToolSetup.Resolve(Settings.DefaultPath(), DiffTools.Detect(), Console.In, Console.Error, Console.IsInputRedirected);
        DiffTools.Launch(diffTool, leftPreview, rightPreview);
    }

    // --commit renders each listed commit; with no flags, the last commit that changed the deck.
    // --pages renders those pages of each commit, as one PNG per page.
    private static IEnumerable<string> RenderRequestedCommits(Request request)
    {
        var deckPath = request.File!;
        var repository = GitRepository.Containing(deckPath);
        var relativePath = repository.RelativePath(deckPath);

        IEnumerable<string> RenderCommit(string sha) => request.Pages is { } pages
            ? CommitPreviews.RenderPages(repository, relativePath, deckPath, sha, pages, request.Renderer)
            : [CommitPreviews.Render(repository, relativePath, sha, request.Renderer)];

        if (request.Commits.Count > 0)
        {
            // Resolve every commit before rendering any, so a typo fails fast.
            var commits = request.Commits.Select(repository.ResolveCommit).ToList();
            foreach (var preview in commits.SelectMany(RenderCommit))
            {
                yield return preview;
            }

            yield break;
        }

        var lastChange = repository.LastCommitThatChanged(relativePath)
            ?? throw new InvalidOperationException($"{relativePath} has not been committed yet.");
        foreach (var preview in RenderCommit(lastChange))
        {
            yield return preview;
        }
    }
}
