namespace Azusayumi.DatasetBuilder
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowGlobalHelp();
                return;
            }

            string command = args[0];
            string[] subArgs = args[1..];

            switch (command)
            {
                case "build":
                    ProcessBuildDataset(subArgs);
                    break;

                default:
                    ShowGlobalHelp();
                    break;
            }
        }

        private static void ProcessBuildDataset(string[] args)
        {
            string currentDirectory = Environment.CurrentDirectory;
            string sourcePath       = Path.Combine(currentDirectory, "games.pgn");
            string outputPath       = Path.Combine(currentDirectory, "dataset.txt");
            bool   append           = false;
            int    maxScore         = 300;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-s":
                    case "--source":
                        if (i + 1 < args.Length) { sourcePath = args[++i]; }
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length) { outputPath = args[++i]; }
                        break;

                    case "-a":
                    case "--append":
                        append = true;
                        break;

                    case "-m":
                    case "--max-score":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int ms))
                        {
                            maxScore = ms;
                        }
                        break;

                    case "-?":
                    case "-h":
                    case "--help":
                        ShowHelp();
                        return;

                    default:
                        Console.WriteLine($"Unknown option: {args[i]}");
                        Console.WriteLine("Help of build command: Azusayumi.DatasetBuilder build --help");
                        return;
                }
            }

            DatasetBuilder builder = new(append, maxScore);
            builder.Build(sourcePath, outputPath);

            static void ShowHelp()
            {
                Console.WriteLine(@"Usage:
  Azusayumi.DatasetBuilder build [options]

Options:
  -s, --source <PATH>           Source games.
                                Default: ./games.pgn

  -o, --output <PATH>           Output file for the generated dataset.
                                Default: ./dataset.txt

  -a, --append                  Append to the output file.

  -m, --max-score <VALUE>       Exclude positions whose absolute static evaluation exceeds this value.
                                Default: 300

  -?, -h, --help                Show help.

Examples:
  Azusayumi.DatasetBuilder build --source selfplay.pgn --output dataset.txt --append --max-score 500");
            }
        }

        private static void ShowGlobalHelp()
        {
            Console.WriteLine(@"Azusayumi.DatasetBuilder

Dataset builder for gradient descent.

Usage:
  Azusayumi.DatasetBuilder <command> [options]

Commands:
  build    Build dataset from PGN file.

Options:
  -?, -h, --help
      Show help and usage information.

Use ""Azusayumi.DatasetBuilder <command> --help"" for more information about a command.");
        }
    }
}
