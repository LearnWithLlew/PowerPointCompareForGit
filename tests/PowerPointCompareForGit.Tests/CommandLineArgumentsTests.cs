namespace PowerPointCompareForGit.Tests;

public class CommandLineArgumentsTests
{
    private static Request Parse(params string[] args)
    {
        var parseResult = CommandLineArguments.CreateRootCommand().Parse(args);
        Assert.Empty(parseResult.Errors);
        return CommandLineArguments.ToRequest(parseResult);
    }

    private static IEnumerable<string> Errors(params string[] args)
        => CommandLineArguments.CreateRootCommand().Parse(args).Errors.Select(error => error.Message);

    [Fact]
    public void FileOnly()
    {
        var request = Parse("deck.pptx");

        Assert.Equal("deck.pptx", request.File);
        Assert.Empty(request.Commits);
        Assert.False(request.Compare);
        Assert.False(request.Reset);
    }

    [Fact]
    public void CommitListIsSplitOnCommas()
    {
        var request = Parse("deck.pptx", "--commit", "sha1, sha2,sha3");

        Assert.Equal(["sha1", "sha2", "sha3"], request.Commits);
    }

    [Fact]
    public void CompareWithNoShas()
    {
        var request = Parse("deck.pptx", "--compare");

        Assert.True(request.Compare);
        Assert.Empty(request.CompareShas);
    }

    [Fact]
    public void CompareWithOneSha()
    {
        var request = Parse("deck.pptx", "--compare", "sha1");

        Assert.Equal(["sha1"], request.CompareShas);
    }

    [Fact]
    public void CompareWithTwoShas()
    {
        var request = Parse("deck.pptx", "--compare", "sha1", "sha2");

        Assert.Equal(["sha1", "sha2"], request.CompareShas);
    }

    [Fact]
    public void PagesAreOptional()
    {
        Assert.Null(Parse("deck.pptx").Pages);
    }

    [Fact]
    public void PagesTakesASinglePage()
    {
        Assert.Equal(new PageRange(40, 40), Parse("deck.pptx", "--pages", "40").Pages);
    }

    [Fact]
    public void PagesTakesARangeAndWorksWithCompare()
    {
        var request = Parse("deck.pptx", "--compare", "sha1", "--pages", "2-25");

        Assert.Equal(new PageRange(2, 25), request.Pages);
        Assert.Equal(["sha1"], request.CompareShas);
    }

    [Fact]
    public void InvalidPagesAreRejected()
    {
        Assert.Contains("--pages must be a page like 40 or a range like 2-25.", Errors("deck.pptx", "--pages", "25-2"));
    }

    [Fact]
    public void RendererDefaultsToDeckCraft()
    {
        Assert.Equal(Renderer.DeckCraft, Parse("deck.pptx").Renderer);
    }

    [Fact]
    public void RendererIsCaseInsensitive()
    {
        Assert.Equal(Renderer.LibreOffice, Parse("deck.pptx", "--renderer", "libreoffice").Renderer);
    }

    [Fact]
    public void UnknownRendererIsRejected()
    {
        Assert.NotEmpty(Errors("deck.pptx", "--renderer", "powerpoint"));
    }

    [Fact]
    public void ResetNeedsNoFile()
    {
        var request = Parse("--reset");

        Assert.True(request.Reset);
        Assert.Null(request.File);
    }

    [Fact]
    public void FileIsRequired()
    {
        Assert.Contains("A .pptx file is required.", Errors("--compare"));
    }

    [Fact]
    public void CommitAndCompareAreExclusive()
    {
        Assert.Contains("--commit and --compare can't be used together.", Errors("deck.pptx", "--commit", "sha1", "--compare"));
    }

    [Fact]
    public void CompareTakesAtMostTwoShas()
    {
        Assert.NotEmpty(Errors("deck.pptx", "--compare", "sha1", "sha2", "sha3"));
    }

    [Fact]
    public void HelpLinksToAgentSkill()
    {
        var output = new StringWriter();
        CommandLineArguments.CreateRootCommand().Parse("--help").Invoke(new() { Output = output });

        Assert.Contains($"AI agents: see {CommandLineArguments.AgentSkillUrl}", output.ToString());
    }
}
