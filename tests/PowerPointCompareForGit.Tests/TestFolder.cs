namespace PowerPointCompareForGit.Tests;

// A temp folder that is deleted when the test ends.
internal sealed class TestFolder : IDisposable
{
    internal TestFolder()
    {
        Path = Directory.CreateTempSubdirectory("pptx-test-").FullName;
    }

    internal string Path { get; }

    internal static string ThreeSlideDeck => System.IO.Path.Combine(AppContext.BaseDirectory, "TestDecks", "three-slides.pptx");

    internal string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
