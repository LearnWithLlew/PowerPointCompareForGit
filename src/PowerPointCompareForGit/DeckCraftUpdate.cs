using System.Text.Json;

namespace PowerPointCompareForGit;

internal static class DeckCraftUpdate
{
    // GitHub is asked at most once a day, so most runs make no network call.
    internal static bool UpdateCheckIsDue(string stampFile, DateTime utcNow)
        => !File.Exists(stampFile) || utcNow - File.GetLastWriteTimeUtc(stampFile) >= TimeSpan.FromDays(1);

    // deckcraft-cli --version prints "deckcraft-cli 0.13.1".
    internal static Version? InstalledVersion(string versionOutput)
        => Version.TryParse(versionOutput.Trim().Split(' ').Last(), out var version) ? version : null;

    // Asks whether to update when latest is newer. With piped input it only prints a notice.
    // Returns true when the user chose to update.
    internal static bool OfferUpdate(Version installed, string latest, UserConsole console)
    {
        if (!Version.TryParse(latest, out var latestVersion) || latestVersion <= installed)
        {
            return false;
        }

        console.Prompt.WriteLine($"DeckCraft {latest} is available (you have {installed}).");
        if (console.InputRedirected)
        {
            console.Prompt.WriteLine("Run CreatePowerpointCheckpoint in a terminal to update it.");
            return false;
        }

        console.Prompt.WriteLine("1. Update (this repo's cached DeckCraft previews are deleted, since the old version drew them)");
        console.Prompt.WriteLine("2. Not now");
        return console.Ask("Choice: ") == "1";
    }

    // Only updates the copy this tool installed; one on PATH or at deckCraftCliPath is the user's to manage.
    // Returns true when it updated. A failed check is silent, so working offline is unaffected.
    internal static bool CheckForUpdate(string cli, UserConsole console)
    {
        var stampFile = Path.Combine(Path.GetDirectoryName(Settings.DefaultPath())!, "deckcraft-update-check.txt");
        if (!IsInstalledByUs(cli) || !UpdateCheckIsDue(stampFile, DateTime.UtcNow))
        {
            return false;
        }

        File.WriteAllText(stampFile, DateTime.UtcNow.ToString("O"));
        if (LatestVersionOrNull() is not { } latest
            || InstalledVersion(ProcessRunner.Run(cli, ["--version"], Path.GetTempPath()).OutputText) is not { } installed
            || !OfferUpdate(installed, latest, console))
        {
            return false;
        }

        DeckCraftSetup.Install(console.Prompt);
        return true;
    }

    private static string? LatestVersionOrNull()
    {
        try
        {
            using var client = DeckCraftRelease.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            return DeckCraftRelease.LatestVersion(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return null;
        }
    }

    private static bool IsInstalledByUs(string cli)
        => Path.GetFullPath(cli).StartsWith(Path.GetFullPath(DeckCraft.InstallFolder()) + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
