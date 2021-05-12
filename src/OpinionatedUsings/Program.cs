using Array = System.Array;
using Console = System.Console;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using System.Collections.Generic;

using System.CommandLine;  // can't alias
using System.Linq; // can't alias


namespace OpinionatedUsings
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        private static bool ScanPaths(IEnumerable<string> paths, bool verbose)
        {
            bool success = true;

            foreach (string path in paths)
            {
                string programText = File.ReadAllText(path);
                var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                    programText);

                List<Inspection.Record> notOkRecords =
                    Inspection.Inspect(tree)
                        .Where(record => record.Errors.Count > 0).ToList();

                if (notOkRecords.Count == 0)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"PASSED: {path}");
                    }
                }
                else
                {
                    success = false;
                    Console.Error.WriteLine($"FAILED: {path}");
                    foreach (var record in notOkRecords)
                    {
                        Console.Error.WriteLine(
                            $" * Line {record.Line + 1}, column {record.Column + 1}:");

                        foreach (var error in record.Errors)
                        {
                            Console.Error.WriteLine($"   * {error}");
                        }
                    }
                }
            }

            return success;
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class Arguments
        {
#pragma warning disable 8618
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            // ReSharper disable CollectionNeverUpdated.Global
            public string[] Inputs { get; set; }
            public string[]? Excludes { get; set; }
            public bool Verbose { get; set; }
            // ReSharper restore CollectionNeverUpdated.Global
            // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore 8618
        }

        private static int Scan(Arguments a)
        {
            string cwd = Directory.GetCurrentDirectory();
            IEnumerable<string> paths = Input.MatchFiles(
                cwd,
                new List<string>(a.Inputs),
                new List<string>(a.Excludes ?? Array.Empty<string>()));

            bool success = ScanPaths(paths, a.Verbose);

            if (!success)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(
                    "One or more using directives in your code base do not conform " +
                    "to the expected style. Please see above.");
                return 1;
            }

            return 0;
        }

        public static int MainWithCode(string[] args)
        {
            var rootCommand = new RootCommand(
                "Examines the using directives in your C# code.")
            {
                new Option<string[]>(
                        new[] {"--inputs", "-i"},
                        "Glob patterns of the files to be inspected")
                    {Required = true},

                new Option<string[]>(
                    new[] {"--excludes", "-e"},
                    "Glob patterns of the files to be excluded"),

                new Option<bool>(
                    new[] {"--verbose"},
                    "If set, makes the console output more verbose"
                )
            };

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(
                (Arguments a) => Scan(a));

            int exitCode = rootCommand.InvokeAsync(args).Result;
            return exitCode;
        }

        public static void Main(string[] args)
        {
            int exitCode = MainWithCode(args);
            Environment.ExitCode = exitCode;
        }
    }
}