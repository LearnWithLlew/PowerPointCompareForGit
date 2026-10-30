using System.Diagnostics;
using System.Text;

namespace PowerPointCompareForGit;

internal sealed record DiffTool(string Name, string Command);

internal static class DiffTools
{
    private sealed record KnownTool(string Name, string[] ExecutableNames, string[] StandardLocations, string Arguments);

    // Executable names and install paths are unverified; see TODO in Project.md.
    private static readonly KnownTool[] Known =
    [
        new("Beyond Compare", ["bcomp", "BComp.exe"],
            ["/Applications/Beyond Compare.app/Contents/MacOS/bcomp", @"C:\Program Files\Beyond Compare 5\BComp.exe", @"C:\Program Files\Beyond Compare 4\BComp.exe"],
            "{file1} {file2}"),
        new("Kaleidoscope", ["ksdiff"], ["/usr/local/bin/ksdiff", "/opt/homebrew/bin/ksdiff"], "{file1} {file2}"),
        new("P4Merge", ["p4merge", "p4merge.exe"],
            ["/Applications/p4merge.app/Contents/MacOS/p4merge", @"C:\Program Files\Perforce\p4merge.exe"],
            "{file1} {file2}"),
        // Not looked up on PATH: ImageMagick also installs a "compare" command.
        new("Araxis Merge", [],
            ["/Applications/Araxis Merge.app/Contents/Utilities/compare", @"C:\Program Files\Araxis\Araxis Merge\Compare.exe"],
            "{file1} {file2}"),
        new("WinMerge", ["WinMergeU.exe"], [@"C:\Program Files\WinMerge\WinMergeU.exe"], "{file1} {file2}")
    ];

    internal static IReadOnlyList<DiffTool> Detect()
        => Known
            .Select(tool => (tool, path: FindExecutable(tool)))
            .Where(found => found.path is not null)
            .Select(found => new DiffTool(found.tool.Name, $"{Quote(found.path!)} {found.tool.Arguments}"))
            .ToList();

    // Starts the diff tool without waiting for it to close.
    internal static void Launch(string command, string file1, string file2)
    {
        var parts = Split(command).Select(part => part.Replace("{file1}", file1).Replace("{file2}", file2)).ToList();
        if (parts.Count == 0)
        {
            throw new InvalidOperationException("The diff tool command is empty.");
        }

        var startInfo = new ProcessStartInfo(parts[0]) { UseShellExecute = false };
        foreach (var argument in parts.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {parts[0]}.");
    }

    // Splits on spaces, keeping "double quoted" parts together, so paths with spaces work.
    internal static IReadOnlyList<string> Split(string command)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var hasPart = false;
        foreach (var c in command)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasPart = true;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasPart)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    hasPart = false;
                }
            }
            else
            {
                current.Append(c);
                hasPart = true;
            }
        }

        if (hasPart)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;

    private static string? FindExecutable(KnownTool tool)
    {
        var onPath = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(folder => tool.ExecutableNames.Select(name => Path.Combine(folder, name)))
            .FirstOrDefault(File.Exists);
        return onPath ?? tool.StandardLocations.FirstOrDefault(File.Exists);
    }
}
