namespace PowerPointCompareForGit;

internal static class CommitPreviews
{
    // Returns the preview PNG for the deck as of the commit, rendering it only if it isn't cached yet.
    internal static string Render(GitRepository repository, string relativePath, string sha, Renderer renderer = Renderer.LibreOffice)
    {
        var previewPath = PreviewPaths.For(repository.Root, relativePath, sha, renderer);
        if (File.Exists(previewPath))
        {
            return previewPath;
        }

        if (!repository.FileExistsAt(sha, relativePath))
        {
            throw new InvalidOperationException($"{relativePath} does not exist in commit {sha[..7]}.");
        }

        var slideRenderer = FindRenderer(renderer);
        var work = Directory.CreateTempSubdirectory("pptx-extract-");
        try
        {
            var deckCopy = Path.Combine(work.FullName, Path.GetFileName(relativePath));
            repository.ExtractFile(sha, relativePath, deckCopy);
            DeckRenderer.Render(slideRenderer, deckCopy, previewPath);
        }
        finally
        {
            work.Delete(recursive: true);
        }

        return previewPath;
    }

    // Renders the deck as it is on disk. Never cached: it is re-rendered on every run.
    internal static string RenderWorkingCopy(GitRepository repository, string relativePath, string deckPath, Renderer renderer = Renderer.LibreOffice)
    {
        if (!File.Exists(deckPath))
        {
            throw new FileNotFoundException($"{deckPath} does not exist.");
        }

        var previewPath = PreviewPaths.For(repository.Root, relativePath, PreviewPaths.WorkingCopy, renderer);
        DeckRenderer.Render(FindRenderer(renderer), deckPath, previewPath);
        return previewPath;
    }

    // version is a commit sha or PreviewPaths.WorkingCopy.
    internal static string Render(GitRepository repository, string relativePath, string deckPath, string version, Renderer renderer = Renderer.LibreOffice)
        => version == PreviewPaths.WorkingCopy
            ? RenderWorkingCopy(repository, relativePath, deckPath, renderer)
            : Render(repository, relativePath, version, renderer);

    // One PNG per page, rendering only the pages that aren't cached yet. version is a commit sha or PreviewPaths.WorkingCopy.
    internal static IReadOnlyList<string> RenderPages(GitRepository repository, string relativePath, string deckPath, string version, PageRange pages, Renderer renderer = Renderer.LibreOffice)
        => WithDeck(repository, relativePath, deckPath, version, deck => RenderPagesOf(repository, relativePath, deck, version, Clamp(pages, deck), renderer));

    // One PNG for the diff tool: the page itself if only one is left after clamping, otherwise the pages stitched together.
    internal static string RenderForCompare(GitRepository repository, string relativePath, string deckPath, string version, PageRange pages, Renderer renderer = Renderer.LibreOffice)
        => WithDeck(repository, relativePath, deckPath, version, deck =>
        {
            var clamped = Clamp(pages, deck);
            if (clamped.IsSinglePage)
            {
                return RenderPagesOf(repository, relativePath, deck, version, clamped, renderer)[0];
            }

            var previewPath = PreviewPaths.ForPages(repository.Root, relativePath, version, clamped, renderer);
            if (version == PreviewPaths.WorkingCopy || !File.Exists(previewPath))
            {
                DeckRenderer.RenderRange(FindRenderer(renderer), deck, clamped, previewPath);
            }

            return previewPath;
        });

    private static IReadOnlyList<string> RenderPagesOf(GitRepository repository, string relativePath, string deck, string version, PageRange pages, Renderer renderer)
    {
        string PathFor(int page) => PreviewPaths.ForPage(repository.Root, relativePath, version, page, renderer);

        var missing = pages.Pages.Where(page => version == PreviewPaths.WorkingCopy || !File.Exists(PathFor(page))).ToList();
        if (missing.Count > 0)
        {
            DeckRenderer.RenderPages(FindRenderer(renderer), deck, missing, PathFor);
        }

        return pages.Pages.Select(PathFor).ToList();
    }

    private static PageRange Clamp(PageRange pages, string deck)
    {
        var slideCount = SlideSplitter.CountSlides(deck);
        return slideCount == 0
            ? throw new InvalidOperationException($"{deck} has no slides.")
            : pages.ClampTo(slideCount);
    }

    // Runs action on the deck as of the version: the file on disk for the working copy, otherwise a temporary copy
    // from git. Which pages exist depends on the deck, so this is needed even when the previews are already cached.
    private static T WithDeck<T>(GitRepository repository, string relativePath, string deckPath, string version, Func<string, T> action)
    {
        if (version == PreviewPaths.WorkingCopy)
        {
            return File.Exists(deckPath) ? action(deckPath) : throw new FileNotFoundException($"{deckPath} does not exist.");
        }

        if (!repository.FileExistsAt(version, relativePath))
        {
            throw new InvalidOperationException($"{relativePath} does not exist in commit {version[..7]}.");
        }

        var work = Directory.CreateTempSubdirectory("pptx-extract-");
        try
        {
            var deckCopy = Path.Combine(work.FullName, Path.GetFileName(relativePath));
            repository.ExtractFile(version, relativePath, deckCopy);
            return action(deckCopy);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static ISlideRenderer FindRenderer(Renderer renderer)
        => renderer == Renderer.DeckCraft
            ? new DeckCraftRenderer(DeckCraft.Find(Settings.Load(Settings.DefaultPath()).DeckCraftCliPath) ?? throw new InvalidOperationException(DeckCraft.NotFoundMessage()))
            : new LibreOfficeRenderer(FindLibreOffice());

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
