namespace PowerPointCompareForGit.Tests;

public class DiffToolSetupTests
{
    private static readonly DiffTool[] Detected = [new("Beyond Compare", "bcomp {file1} {file2}"), new("P4Merge", "p4merge {file1} {file2}")];

    private static string Resolve(TestFolder folder, string answers, bool inputRedirected = false)
        => DiffToolSetup.Resolve(folder.Combine("settings.json"), Detected, new StringReader(answers), new StringWriter(), inputRedirected);

    [Fact]
    public void UsesSavedTool()
    {
        using var folder = new TestFolder();
        new Settings(DiffTool: "saved {file1} {file2}").Save(folder.Combine("settings.json"));

        Assert.Equal("saved {file1} {file2}", Resolve(folder, ""));
    }

    [Fact]
    public void PickingADetectedToolSavesIt()
    {
        using var folder = new TestFolder();

        Assert.Equal("p4merge {file1} {file2}", Resolve(folder, "2\n"));
        Assert.Equal("p4merge {file1} {file2}", Settings.Load(folder.Combine("settings.json")).DiffTool);
    }

    [Fact]
    public void CustomCommandIsSaved()
    {
        using var folder = new TestFolder();

        Assert.Equal("mytool -x {file1} {file2}", Resolve(folder, "3\nmytool -x {file1} {file2}\n"));
        Assert.Equal("mytool -x {file1} {file2}", Settings.Load(folder.Combine("settings.json")).DiffTool);
    }

    [Fact]
    public void CustomCommandNeedsBothPlaceholders()
    {
        using var folder = new TestFolder();

        Assert.Throws<InvalidOperationException>(() => Resolve(folder, "3\nmytool {file1}\n"));
    }

    [Fact]
    public void InvalidChoiceFails()
    {
        using var folder = new TestFolder();

        Assert.Throws<InvalidOperationException>(() => Resolve(folder, "9\n"));
    }

    [Fact]
    public void PipedInputFailsWithInstructionsInsteadOfPrompting()
    {
        using var folder = new TestFolder();

        var error = Assert.Throws<InvalidOperationException>(() => Resolve(folder, "1\n", inputRedirected: true));
        Assert.Contains(folder.Combine("settings.json"), error.Message);
        Assert.False(File.Exists(folder.Combine("settings.json")));
    }

    [Fact]
    public void SettingsAreSavedAsCamelCase()
    {
        using var folder = new TestFolder();
        new Settings(DiffTool: "x", LibreOfficePath: "y").Save(folder.Combine("settings.json"));

        var json = File.ReadAllText(folder.Combine("settings.json"));
        Assert.Contains("\"diffTool\"", json);
        Assert.Contains("\"libreOfficePath\"", json);
    }

    [Fact]
    public void SplitKeepsQuotedPathsTogether()
    {
        Assert.Equal(
            ["/Applications/Beyond Compare.app/Contents/MacOS/bcomp", "-ro", "{file1}", "{file2}"],
            DiffTools.Split("\"/Applications/Beyond Compare.app/Contents/MacOS/bcomp\" -ro {file1}  {file2}"));
    }
}
