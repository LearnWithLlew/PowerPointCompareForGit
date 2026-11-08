namespace PowerPointCompareForGit;

internal static class DiffToolSetup
{
    private const string CustomExample = "mytool --flags {file1} {file2}";

    // Returns the saved diff tool command, or asks the user to pick one and saves it.
    // When input is piped there is no one to ask, so it fails with instructions instead.
    internal static string Resolve(string settingsPath, IReadOnlyList<DiffTool> detected, UserConsole console)
    {
        var settings = Settings.Load(settingsPath);
        if (!string.IsNullOrWhiteSpace(settings.DiffTool))
        {
            return settings.DiffTool;
        }

        if (console.InputRedirected)
        {
            throw new InvalidOperationException(
                $"No diff tool configured. Run CreatePowerpointCheckpoint --compare once interactively (without piped input) to pick one, " +
                $"or set \"diffTool\": \"{CustomExample}\" in {settingsPath}.");
        }

        var command = AskForCommand(detected, console);
        (settings with { DiffTool = command }).Save(settingsPath);
        console.Prompt.WriteLine($"Saved to {settingsPath}");
        return command;
    }

    private static string AskForCommand(IReadOnlyList<DiffTool> detected, UserConsole console)
    {
        console.Prompt.WriteLine("Which diff tool should open the PNGs?");
        for (var i = 0; i < detected.Count; i++)
        {
            console.Prompt.WriteLine($"{i + 1}. {detected[i].Name} ({detected[i].Command})");
        }

        var customChoice = detected.Count + 1;
        console.Prompt.WriteLine($"{customChoice}. Custom (example: {CustomExample})");
        var answer = console.Ask("Choice: ");

        if (int.TryParse(answer, out var choice) && choice >= 1 && choice <= detected.Count)
        {
            return detected[choice - 1].Command;
        }

        if (answer == customChoice.ToString())
        {
            return AskForCustomCommand(console);
        }

        throw new InvalidOperationException($"'{answer}' is not one of the choices.");
    }

    private static string AskForCustomCommand(UserConsole console)
    {
        var command = console.Ask("Command (use {file1} and {file2}): ") ?? "";
        if (!command.Contains("{file1}") || !command.Contains("{file2}"))
        {
            throw new InvalidOperationException("The command must contain {file1} and {file2}.");
        }

        return command;
    }
}
