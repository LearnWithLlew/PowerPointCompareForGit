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
                Run(request);
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

    private static void Run(Request request)
    {
        if (request.Reset)
        {
            Reset(Settings.DefaultPath());
            return;
        }

        if (request.ClearCache)
        {
            ClearCache(request.File ?? ".");
            return;
        }

        if (request.Compare)
        {
            Compare(request);
            return;
        }

        foreach (var previewPath in RenderRequestedCommits(request))
        {
            Console.WriteLine(previewPath);
        }
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

    // path is the deck or any folder in the repo.
    private static void ClearCache(string path)
    {
        var root = GitRepository.Containing(path).Root;
        var folder = PreviewPaths.CacheFolder(root);
        Console.WriteLine(PreviewPaths.Clear(root)
            ? $"Deleted {folder}"
            : $"No cached previews to delete ({folder} does not exist).");
    }

    // Runs before any preview is read, so an update can't delete a preview this run is about to open.
    private static void CheckForDeckCraftUpdate(GitRepository repository, Renderer renderer)
    {
        if (renderer == Renderer.DeckCraft
            && DeckCraftSetup.CheckForUpdate(CommitPreviews.FindDeckCraft(), UserConsole.Standard()))
        {
            PreviewPaths.ClearDeckCraft(repository.Root);
        }
    }

    // Renders both versions, prints their paths (older first) and opens them in the diff tool.
    private static void Compare(Request request)
    {
        var deck = DeckInRepo.At(request.File!);
        CheckForDeckCraftUpdate(deck.Repository, request.Renderer);
        var (left, right) = ComparePlan.Choose(deck.Repository, deck.RelativePath, request.CompareShas);
        var previews = new CommitPreviews(deck, request.Renderer);
        if (request.Selective)
        {
            CompareSelective(previews, left, right);
            return;
        }

        string RenderVersion(string version) => request.Pages is { } pages
            ? previews.RenderForCompare(version, pages)
            : previews.Render(version);

        var leftPreview = RenderVersion(left);
        var rightPreview = RenderVersion(right);
        Console.WriteLine(leftPreview);
        Console.WriteLine(rightPreview);

        OpenInDiffTool(leftPreview, rightPreview);
    }

    // Opens only the slides that changed. Anything the images can't show goes to stderr, so stdout stays a clean list of PNG paths.
    private static void CompareSelective(CommitPreviews previews, string left, string right)
    {
        var selective = previews.RenderSelective(left, right);
        foreach (var message in selective.Alignment.Messages(VersionName(left), VersionName(right)))
        {
            Console.Error.WriteLine(message);
        }

        if (selective.Images is { } images)
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
        var diffTool = DiffToolSetup.Resolve(Settings.DefaultPath(), DiffTools.Detect(), UserConsole.Standard());
        DiffTools.Launch(diffTool, leftPreview, rightPreview);
    }

    // --commit renders each listed commit; with no flags, the last commit that changed the deck.
    // --pages renders those pages of each commit, as one PNG per page.
    private static IEnumerable<string> RenderRequestedCommits(Request request)
    {
        var deck = DeckInRepo.At(request.File!);
        CheckForDeckCraftUpdate(deck.Repository, request.Renderer);
        var previews = new CommitPreviews(deck, request.Renderer);

        IEnumerable<string> RenderCommit(string sha) => request.Pages is { } pages
            ? previews.RenderPages(sha, pages)
            : [previews.RenderCommit(sha)];

        if (request.Commits.Count > 0)
        {
            // Resolve every commit before rendering any, so a typo fails fast.
            var commits = request.Commits.Select(deck.Repository.ResolveCommit).ToList();
            foreach (var preview in commits.SelectMany(RenderCommit))
            {
                yield return preview;
            }

            yield break;
        }

        var lastChange = deck.Repository.LastCommitThatChanged(deck.RelativePath)
            ?? throw new InvalidOperationException($"{deck.RelativePath} has not been committed yet.");
        foreach (var preview in RenderCommit(lastChange))
        {
            yield return preview;
        }
    }
}
