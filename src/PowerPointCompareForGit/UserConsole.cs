namespace PowerPointCompareForGit;

internal sealed record UserConsole(TextReader Input, TextWriter Prompt, bool InputRedirected)
{
    // Prompts go to stderr so stdout stays a clean list of PNG paths.
    internal static UserConsole Standard() => new(Console.In, Console.Error, Console.IsInputRedirected);

    internal string? Ask(string question)
    {
        Prompt.Write(question);
        return Input.ReadLine()?.Trim();
    }
}
