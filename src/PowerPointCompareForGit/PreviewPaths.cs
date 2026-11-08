namespace PowerPointCompareForGit;

// deckPath may be absolute or relative to repoRoot.
internal sealed class PreviewPaths(string repoRoot, string deckPath, Renderer renderer = Renderer.LibreOffice)
{
    internal const string WorkingCopy = "working";
    private const int ShortShaLength = 7;

    // <repoRoot>/.git/pptx-previews/<deck folder relative to repoRoot>/<deck file name>.<short sha or "working">.png
    // DeckCraft adds .deckcraft after the version, so the two renderers' previews never overwrite each other.
    internal string For(string version) => Build(version, "");

    // Same folder, with .page.<n> before the .png.
    internal string ForPage(string version, int page) => Build(version, $".page.{page}");

    // Same folder, with .pages.<first>-<last> before the .png.
    internal string ForPages(string version, PageRange pages) => Build(version, $".pages.{pages.First}-{pages.Last}");

    // Same folder, with .selective-vs.<other version> before the .png, because what's drawn depends on both versions.
    internal string ForSelective(string version, string otherVersion) => Build(version, $".selective-vs.{Label(otherVersion)}");

    // Every preview of every deck in the repo is somewhere under here.
    internal static string CacheFolder(string repoRoot) => Path.Combine(Path.GetFullPath(repoRoot), ".git", "pptx-previews");

    // Returns false when there was nothing to delete.
    internal static bool Clear(string repoRoot)
    {
        var folder = CacheFolder(repoRoot);
        if (!Directory.Exists(folder))
        {
            return false;
        }

        Directory.Delete(folder, recursive: true);
        return true;
    }

    // Deletes only DeckCraft's previews, e.g. after DeckCraft is updated, leaving LibreOffice's.
    internal static void ClearDeckCraft(string repoRoot)
    {
        var folder = CacheFolder(repoRoot);
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var preview in Directory.EnumerateFiles(folder, "*.deckcraft*.png", SearchOption.AllDirectories))
        {
            File.Delete(preview);
        }
    }

    private static string Label(string version)
        => version == WorkingCopy ? WorkingCopy : version[..Math.Min(ShortShaLength, version.Length)];

    private string Build(string version, string suffix)
    {
        var root = Path.GetFullPath(repoRoot);
        var deck = Path.GetFullPath(deckPath, root);
        var relativeDeck = Path.GetRelativePath(root, deck);
        if (relativeDeck.StartsWith("..") || Path.IsPathRooted(relativeDeck))
        {
            throw new ArgumentException($"{deck} is not inside the repository {root}.", nameof(deckPath));
        }

        var rendererLabel = renderer == Renderer.LibreOffice ? "" : $".{renderer.ToString().ToLowerInvariant()}";
        var folder = Path.Combine(CacheFolder(root), Path.GetDirectoryName(relativeDeck) ?? "");
        return Path.Combine(folder, $"{Path.GetFileName(deck)}.{Label(version)}{rendererLabel}{suffix}.png");
    }
}
