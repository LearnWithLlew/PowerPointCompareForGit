namespace PowerPointCompareForGit;

internal static class DiffToolSetup
{
    private const string CustomExample = "mytool --flags {file1} {file2}";

    // Returns the saved diff tool command, or asks the user to pick one and saves it.
    // When input is piped there is no one to ask, so it fails with instructions instead.
    internal static string Resolve(string settingsPath, IReadOnlyList<DiffTool> detected, TextReader input, TextWriter prompt, bool inputRedirected)
    {
        var settings = Settings.Load(settingsPath);
        if (!string.IsNullOrWhiteSpace(settings.DiffTool))
        {
            return settings.DiffTool;
        }

        if (inputRedirected)
        {
            throw new InvalidOperationException(
                $"No diff tool configured. Run CreatePowerpointCheckpoint --compare once interactively (without piped input) to pick one, " +
                $"or set \"diffTool\": \"{CustomExample}\" in {settingsPath}.");
        }

        prompt.WriteLine("Which diff tool should open the PNGs?");
        for (var i = 0; i < detected.Count; i++)
        {
            prompt.WriteLine($"{i + 1}. {detected[i].Name} ({detected[i].Command})");
        }

        var customChoice = detected.Count + 1;
        prompt.WriteLine($"{customChoice}. Custom (example: {CustomExample})");
        prompt.Write("Choice: ");
        var answer = input.ReadLine()?.Trim();

        string command;
        if (int.TryParse(answer, out var choice) && choice >= 1 && choice <= detected.Count)
        {
            command = detected[choice - 1].Command;
        }
        else if (answer == customChoice.ToString())
        {
            prompt.Write("Command (use {file1} and {file2}): ");
            command = input.ReadLine()?.Trim() ?? "";
            if (!command.Contains("{file1}") || !command.Contains("{file2}"))
            {
                throw new InvalidOperationException("The command must contain {file1} and {file2}.");
            }
        }
        else
        {
            throw new InvalidOperationException($"'{answer}' is not one of the choices.");
        }

        (settings with { DiffTool = command }).Save(settingsPath);
        prompt.WriteLine($"Saved to {settingsPath}");
        return command;
    }
}
