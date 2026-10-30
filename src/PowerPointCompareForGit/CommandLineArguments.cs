using System.CommandLine;

namespace PowerPointCompareForGit;

// What the user asked for. Compare is true for --compare, with 0, 1 or 2 shas in CompareShas.
// Pages is null when the whole deck is wanted.
internal sealed record Request(string? File, IReadOnlyList<string> Commits, bool Compare, IReadOnlyList<string> CompareShas, PageRange? Pages, bool Reset);

internal static class CommandLineArguments
{
    internal const string AgentSkillUrl = "https://github.com/LearnWithLlew/PowerPointCompareForGit/blob/main/skills/powerpoint-compare/SKILL.md";

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

    private static readonly Option<string?> PagesOption = new("--pages")
    {
        Description = "Only these slides, e.g. 40 or 2-25, one PNG per slide (with --compare, a range is one stitched PNG per version). Past the end means the last slide.",
        HelpName = "pages"
    };

    private static readonly Option<bool> ResetOption = new("--reset")
    {
        Description = "Clear local settings."
    };

    internal static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand($"""
            Render PowerPoint decks from git history to PNG and compare them.

            AI agents: see {AgentSkillUrl}
            """)
        {
            FileArgument, CommitOption, CompareOption, PagesOption, ResetOption
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

            if (result.GetValue(PagesOption) is { } pages && PageRange.Parse(pages) is null)
            {
                result.AddError("--pages must be a page like 40 or a range like 2-25.");
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
            parseResult.GetValue(PagesOption) is { } pages ? PageRange.Parse(pages) : null,
            parseResult.GetValue(ResetOption));
    }
}
