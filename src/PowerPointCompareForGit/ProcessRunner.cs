using System.Diagnostics;
using System.Text;

namespace PowerPointCompareForGit;

internal sealed record ProcessResult(int ExitCode, byte[] Output, string Error)
{
    internal string OutputText => Encoding.UTF8.GetString(Output).Trim();
}

internal static class ProcessRunner
{
    // Output is kept as bytes so binary output (e.g. `git show` of a .pptx) survives.
    internal static ProcessResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var error = process.StandardError.ReadToEndAsync();
        using var output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToArray(), error.Result);
    }
}
