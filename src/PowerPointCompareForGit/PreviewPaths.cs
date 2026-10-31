namespace PowerPointCompareForGit;

internal static class PreviewPaths
{
    internal const string WorkingCopy = "working";
    private const int ShortShaLength = 7;

    // <repoRoot>/.git/pptx-previews/<deck folder relative to repoRoot>/<deck file name>.<short sha or "working">.png
    // DeckCraft adds .deckcraft after the version, so the two renderers' previews never overwrite each other.
    // deckPath may be absolute or relative to repoRoot.
    internal static string For(string repoRoot, string deckPath, string version, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, "");

    // Same folder, with .page.<n> before the .png.
    internal static string ForPage(string repoRoot, string deckPath, string version, int page, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, $".page.{page}");

    // Same folder, with .pages.<first>-<last> before the .png.
    internal static string ForPages(string repoRoot, string deckPath, string version, PageRange pages, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, $".pages.{pages.First}-{pages.Last}");

    // Same folder, with .selective-vs.<other version> before the .png, because what's drawn depends on both versions.
    internal static string ForSelective(string repoRoot, string deckPath, string version, string otherVersion, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, $".selective-vs.{Label(otherVersion)}");

    private static string Label(string version)
        => version == WorkingCopy ? WorkingCopy : version[..Math.Min(ShortShaLength, version.Length)];

    private static string Build(string repoRoot, string deckPath, string version, Renderer renderer, string suffix)
    {
        var root = Path.GetFullPath(repoRoot);
        var deck = Path.GetFullPath(deckPath, root);
        var relativeDeck = Path.GetRelativePath(root, deck);
        if (relativeDeck.StartsWith("..") || Path.IsPathRooted(relativeDeck))
        {
            throw new ArgumentException($"{deck} is not inside the repository {root}.", nameof(deckPath));
        }

        var rendererLabel = renderer == Renderer.LibreOffice ? "" : $".{renderer.ToString().ToLowerInvariant()}";
        var folder = Path.Combine(root, ".git", "pptx-previews", Path.GetDirectoryName(relativeDeck) ?? "");
        return Path.Combine(folder, $"{Path.GetFileName(deck)}.{Label(version)}{rendererLabel}{suffix}.png");
    }
}
