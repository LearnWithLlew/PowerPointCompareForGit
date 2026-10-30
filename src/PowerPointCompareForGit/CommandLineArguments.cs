using System.CommandLine;

namespace PowerPointCompareForGit;

// What the user asked for. Compare is true for --compare, with 0, 1 or 2 shas in CompareShas.
internal sealed record Request(string? File, IReadOnlyList<string> Commits, bool Compare, IReadOnlyList<string> CompareShas, bool Reset);

internal static class CommandLineArguments
{
    private static readonly Argument<string?> FileArgument = new("file")
    {
        Description = "The .pptx file.",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<string?> CommitOption = new("--commit")
    {
        Description = "Comma-separated commits to render, e.g. sha1,sha2,sha3.",
        HelpName = "shas"
    };

    private static readonly Option<string[]> CompareOption = new("--compare")
    {
        Description = "Compare two versions in the diff tool. No sha: working copy vs last change. One sha: working copy vs it.",
        HelpName = "sha",
        Arity = new ArgumentArity(0, 2),
        AllowMultipleArgumentsPerToken = true
    };

    private static readonly Option<bool> ResetOption = new("--reset")
    {
        Description = "Clear local settings."
    };

    internal static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Render PowerPoint decks from git history to PNG and compare them.")
        {
            FileArgument, CommitOption, CompareOption, ResetOption
        };
        rootCommand.Validators.Add(result =>
        {
            if (result.GetResult(ResetOption) is not null)
            {
                return;
            }

            if (result.GetValue(FileArgument) is null)
            {
                result.AddError("A .pptx file is required.");
            }

            if (result.GetResult(CommitOption) is not null && result.GetResult(CompareOption) is not null)
            {
                result.AddError("--commit and --compare can't be used together.");
            }
        });
        return rootCommand;
    }

    internal static Request ToRequest(ParseResult parseResult)
    {
        var commits = parseResult.GetValue(CommitOption)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        return new Request(
            parseResult.GetValue(FileArgument),
            commits,
            parseResult.GetResult(CompareOption) is not null,
            parseResult.GetValue(CompareOption) ?? [],
            parseResult.GetValue(ResetOption));
    }
}
