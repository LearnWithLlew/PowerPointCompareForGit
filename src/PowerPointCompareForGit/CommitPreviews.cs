namespace PowerPointCompareForGit;

// Images is null when no slide changed.
internal sealed record SelectivePreviews(SlideAlignment Alignment, (string Left, string Right)? Images);

internal sealed class CommitPreviews(DeckInRepo deck, Renderer renderer = Renderer.LibreOffice)
{
    private readonly PreviewPaths paths = new(deck.Repository.Root, deck.RelativePath, renderer);

    // Returns the preview PNG for the deck as of the commit, rendering it only if it isn't cached yet.
    internal string RenderCommit(string sha)
    {
        var previewPath = paths.For(sha);
        if (File.Exists(previewPath))
        {
            return previewPath;
        }

        if (!deck.Repository.FileExistsAt(sha, deck.RelativePath))
        {
            throw new InvalidOperationException($"{deck.RelativePath} does not exist in commit {sha[..7]}.");
        }

        var deckRenderer = FindRenderer();
        var work = Directory.CreateTempSubdirectory("pptx-extract-");
        try
        {
            var deckCopy = Path.Combine(work.FullName, Path.GetFileName(deck.RelativePath));
            deck.Repository.ExtractFile(sha, deck.RelativePath, deckCopy);
            deckRenderer.Render(deckCopy, previewPath);
        }
        finally
        {
            work.Delete(recursive: true);
        }

        return previewPath;
    }

    // Renders the deck as it is on disk. Never cached: it is re-rendered on every run.
    internal string RenderWorkingCopy()
    {
        if (!File.Exists(deck.WorkingCopyPath))
        {
            throw new FileNotFoundException($"{deck.WorkingCopyPath} does not exist.");
        }

        var previewPath = paths.For(PreviewPaths.WorkingCopy);
        FindRenderer().Render(deck.WorkingCopyPath, previewPath);
        return previewPath;
    }

    // version is a commit sha or PreviewPaths.WorkingCopy.
    internal string Render(string version)
        => version == PreviewPaths.WorkingCopy ? RenderWorkingCopy() : RenderCommit(version);

    // One PNG per page, rendering only the pages that aren't cached yet. version is a commit sha or PreviewPaths.WorkingCopy.
    internal IReadOnlyList<string> RenderPages(string version, PageRange pages)
        => deck.AsOf(version, deckFile => RenderPagesOf(deckFile, version, Clamp(pages, deckFile)));

    // One PNG for the diff tool: the page itself if only one is left after clamping, otherwise the pages stitched together.
    internal string RenderForCompare(string version, PageRange pages)
        => deck.AsOf(version, deckFile =>
        {
            var clamped = Clamp(pages, deckFile);
            if (clamped.IsSinglePage)
            {
                return RenderPagesOf(deckFile, version, clamped)[0];
            }

            var previewPath = paths.ForPages(version, clamped);
            if (version == PreviewPaths.WorkingCopy || !File.Exists(previewPath))
            {
                FindRenderer().RenderRange(deckFile, clamped, previewPath);
            }

            return previewPath;
        });

    // Compares the versions and draws only the slides that changed, reusing the images if both are cached.
    // The decks are always read, because the moves come from them. There are no images when nothing changed.
    internal SelectivePreviews RenderSelective(string left, string right)
        => deck.AsOf(left, leftDeck => deck.AsOf(right, rightDeck =>
        {
            var alignment = SlideAlignment.Of(SlideFingerprints.Of(leftDeck), SlideFingerprints.Of(rightDeck));
            if (alignment.Rows.Count == 0)
            {
                return new SelectivePreviews(alignment, null);
            }

            var leftPreview = paths.ForSelective(left, right);
            var rightPreview = paths.ForSelective(right, left);
            if (left == PreviewPaths.WorkingCopy || right == PreviewPaths.WorkingCopy || !File.Exists(leftPreview) || !File.Exists(rightPreview))
            {
                FindRenderer().RenderAligned(leftDeck, rightDeck, alignment.Rows, leftPreview, rightPreview);
            }

            return new SelectivePreviews(alignment, (leftPreview, rightPreview));
        }));

    private IReadOnlyList<string> RenderPagesOf(string deckFile, string version, PageRange pages)
    {
        string PathFor(int page) => paths.ForPage(version, page);

        var missing = pages.Pages.Where(page => version == PreviewPaths.WorkingCopy || !File.Exists(PathFor(page))).ToList();
        if (missing.Count > 0)
        {
            FindRenderer().RenderPages(deckFile, missing, PathFor);
        }

        return pages.Pages.Select(PathFor).ToList();
    }

    private static PageRange Clamp(PageRange pages, string deckFile)
    {
        var slideCount = SlideSplitter.CountSlides(deckFile);
        return slideCount == 0
            ? throw new InvalidOperationException($"{deckFile} has no slides.")
            : pages.ClampTo(slideCount);
    }

    private DeckRenderer FindRenderer()
        => new(renderer == Renderer.DeckCraft
            ? new DeckCraftRenderer(FindDeckCraft())
            : new LibreOfficeRenderer(FindLibreOffice()));

    // Prompts go to stderr so stdout stays a clean list of PNG paths.
    internal static string FindDeckCraft()
    {
        var configuredPath = Settings.Load(Settings.DefaultPath()).DeckCraftCliPath;
        return DeckCraftSetup.Resolve(
            () => DeckCraft.Find(configuredPath),
            Console.In,
            Console.Error,
            Console.IsInputRedirected,
            () => DeckCraftSetup.Install(Console.Error));
    }

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
