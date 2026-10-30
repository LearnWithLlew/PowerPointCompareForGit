namespace PowerPointCompareForGit;

internal sealed class GitRepository
{
    private GitRepository(string root)
    {
        Root = root;
    }

    internal string Root { get; }

    internal static GitRepository Containing(string path)
    {
        var result = ProcessRunner.Run("git", ["rev-parse", "--show-toplevel"], DirectoryOf(path));
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"{Path.GetFullPath(path)} is not inside a git repository.");
        }

        return new GitRepository(Path.GetFullPath(result.OutputText));
    }

    // The path relative to the repo root, with forward slashes, as git expects.
    // Asks git for the prefix instead of comparing paths, so symlinked folders (e.g. /tmp on Mac) still work.
    internal string RelativePath(string path)
    {
        var prefix = ProcessRunner.Run("git", ["rev-parse", "--show-prefix"], DirectoryOf(path)).OutputText;
        return prefix + Path.GetFileName(path);
    }

    internal string? LastCommitThatChanged(string relativePath)
    {
        var sha = Run("log", "-1", "--format=%H", "--", relativePath).OutputText;
        return sha.Length == 0 ? null : sha;
    }

    internal void ExtractFile(string sha, string relativePath, string destination)
        => File.WriteAllBytes(destination, Run("show", $"{sha}:{relativePath}").Output);

    private ProcessResult Run(params string[] arguments)
    {
        var result = ProcessRunner.Run("git", arguments, Root);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {result.Error.Trim()}");
        }

        return result;
    }

    private static string DirectoryOf(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath)!;
    }
}
