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
}
