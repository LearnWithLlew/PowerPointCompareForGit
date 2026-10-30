namespace PowerPointCompareForGit;

public static class Program
{
    public static int Main(string[] args)
    {
        var rootCommand = CommandLineArguments.CreateRootCommand();
        rootCommand.SetAction(parseResult =>
        {
            var request = CommandLineArguments.ToRequest(parseResult);
            Console.Error.WriteLine($"Not implemented yet: {request}");
            return 1;
        });

        return rootCommand.Parse(args).Invoke();
    }
}
