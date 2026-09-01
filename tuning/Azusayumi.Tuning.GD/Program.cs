namespace Azusayumi.Tuning.GD
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
                case "train":
                    ProcessTrainWeights(subArgs);
                    break;

                default:
                    ShowGlobalHelp();
                    break;
            }
        }

        private static void ProcessTrainWeights(string[] args)
        {
            string currentDirectory = Environment.CurrentDirectory;
            string datasetPath      = Path.Combine(currentDirectory, "dataset.txt");
            string outputPath       = Path.Combine(currentDirectory, "weights.txt");
            int    epochs           = 100;
            double learningRate     = 0.1;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-d":
                    case "--dataset":
                        if (i + 1 < args.Length) { datasetPath = args[++i]; }
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length) { outputPath = args[++i]; }
                        break;

                    case "-l":
                    case "--learning-rate":
                        if (i + 1 < args.Length && double.TryParse(args[++i], out double lr))
                        {
                            learningRate = lr;
                        }
                        break;

                    case "-e":
                    case "--epochs":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int e))
                        {
                            epochs = e;
                        }
                        break;

                    case "-?":
                    case "-h":
                    case "--help":
                        ShowHelp();
                        return;

                    default:
                        Console.WriteLine($"Unknown option: {args[i]}");
                        Console.WriteLine("Help of train command: Azusayumi.Tuning.GD train --help");
                        return;
                }
            }

            Dataset dataset = new(datasetPath);
            Weights weights = new();
            Tuner   tuner   = new(learningRate, epochs);
            tuner.Tune(dataset, weights);
            weights.Save(outputPath);

            Console.WriteLine("Tuning complete.");
            Console.WriteLine($"Saved to {outputPath}");

            static void ShowHelp()
            {
                Console.WriteLine(@"Usage:
  Azusayumi.Tuning.GD train [options]

Options:
  -d, --dataset <PATH>              Training dataset.
                                    Default: ./dataset.txt

  -o, --output <PATH>               Output file for optimized weights.
                                    Default: ./weights.txt

  -l, --learning-rate <VALUE>       Learning rate.
                                    Default: 0.1

  -e, --epochs <COUNT>              Number of epochs.
                                    Default: 100

  -?, -h, --help                    Show help.

Examples:
  Azusayumi.Tuning.GD train --dataset training.txt --output tuned.txt --learning-rate 0.01 --epochs 200");
            }
        }

        private static void ShowGlobalHelp()
        {
            Console.WriteLine(@"Azusayumi.Tuning.GD

Gradient-descent tuning tools for Azusayumi evaluation parameters.

Usage:
  Azusayumi.Tuning.GD <command> [options]

Commands:
  train    Optimize evaluation weights using gradient descent.

Options:
  -?, -h, --help
      Show help and usage information.

Use ""Azusayumi.Tuning.GD <command> --help"" for more information about a command.");
        }
    }
}
