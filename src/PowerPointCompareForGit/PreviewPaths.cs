namespace PowerPointCompareForGit;

internal static class PreviewPaths
{
    internal const string WorkingCopy = "working";
    private const int ShortShaLength = 7;

    // <repoRoot>/.git/pptx-previews/<deck folder relative to repoRoot>/<deck file name>.<short sha or "working">.png
    // Renderers other than LibreOffice add .<renderer> after the version, so their previews never overwrite each other.
    // deckPath may be absolute or relative to repoRoot.
    internal static string For(string repoRoot, string deckPath, string version, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, "");

    // Same folder, with .page.<n> before the .png.
    internal static string ForPage(string repoRoot, string deckPath, string version, int page, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, $".page.{page}");

    // Same folder, with .pages.<first>-<last> before the .png.
    internal static string ForPages(string repoRoot, string deckPath, string version, PageRange pages, Renderer renderer = Renderer.LibreOffice)
        => Build(repoRoot, deckPath, version, renderer, $".pages.{pages.First}-{pages.Last}");

    private static string Build(string repoRoot, string deckPath, string version, Renderer renderer, string suffix)
    {
        var root = Path.GetFullPath(repoRoot);
        var deck = Path.GetFullPath(deckPath, root);
        var relativeDeck = Path.GetRelativePath(root, deck);
        if (relativeDeck.StartsWith("..") || Path.IsPathRooted(relativeDeck))
        {
            throw new ArgumentException($"{deck} is not inside the repository {root}.", nameof(deckPath));
        }

        var label = version == WorkingCopy ? WorkingCopy : version[..Math.Min(ShortShaLength, version.Length)];
        var rendererLabel = renderer == Renderer.LibreOffice ? "" : $".{renderer.ToString().ToLowerInvariant()}";
        var folder = Path.Combine(root, ".git", "pptx-previews", Path.GetDirectoryName(relativeDeck) ?? "");
        return Path.Combine(folder, $"{Path.GetFileName(deck)}.{label}{rendererLabel}{suffix}.png");
    }
}
