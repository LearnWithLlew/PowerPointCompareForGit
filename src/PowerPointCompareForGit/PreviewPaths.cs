namespace PowerPointCompareForGit;

internal static class PreviewPaths
{
    internal const string WorkingCopy = "working";
    private const int ShortShaLength = 7;

    // <repoRoot>/.git/pptx-previews/<deck folder relative to repoRoot>/<deck file name>.<short sha or "working">.png
    // deckPath may be absolute or relative to repoRoot.
    internal static string For(string repoRoot, string deckPath, string version)
    {
        var root = Path.GetFullPath(repoRoot);
        var deck = Path.GetFullPath(deckPath, root);
        var relativeDeck = Path.GetRelativePath(root, deck);
        if (relativeDeck.StartsWith("..") || Path.IsPathRooted(relativeDeck))
        {
            throw new ArgumentException($"{deck} is not inside the repository {root}.", nameof(deckPath));
        }

        var label = version == WorkingCopy ? WorkingCopy : version[..Math.Min(ShortShaLength, version.Length)];
        var folder = Path.Combine(root, ".git", "pptx-previews", Path.GetDirectoryName(relativeDeck) ?? "");
        return Path.Combine(folder, $"{Path.GetFileName(deck)}.{label}.png");
    }
}
