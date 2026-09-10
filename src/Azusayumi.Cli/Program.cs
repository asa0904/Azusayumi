namespace Azusayumi.Cli
{
    internal class Program
    {
        private static bool _isRunning = true;
        private static readonly UciEngine _engine = new();

        internal static void Main()
        {
            Console.InputEncoding  = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            _engine.Clear();

            while (_isRunning)
            {
                string? input = Console.ReadLine();
                if (input == null) { continue; }

                try
                {
                    ProcessCommand(input);
                }
                catch (Exception ex)
                {
                    _engine.Clear();
                    Console.WriteLine($"An error occurred while processing the UCI command: {ex.Message}");
                }
            }
        }

        private static void ProcessCommand(string command)
        {
            ReadOnlySpan<char> tokens = command.AsSpan().Trim().ConsumeTo(' ', out ReadOnlySpan<char> token);

            if (token.SequenceEqual("position"))
            {
                SetPosition(tokens);
            }
            else if (token.SequenceEqual("go"))
            {
                StartSearching(tokens);
            }
            else if (token.SequenceEqual("stop"))
            {
                _engine.Stop();
            }
            else if (token.SequenceEqual("ponderhit"))
            {
                _engine.StopPondering();
            }
            else if (token.SequenceEqual("uci"))
            {
                UciEngine.PrintUciInfo();
                Console.WriteLine("uciok");
            }
            else if (token.SequenceEqual("setoption"))
            {
                SetOption(tokens);
            }
            else if (token.SequenceEqual("isready"))
            {
                Console.WriteLine("readyok");
            }
            else if (token.SequenceEqual("ucinewgame"))
            {
                _engine.Clear();
            }
            else if (token.SequenceEqual("quit"))
            {
                _engine.Quit();
                _isRunning = false;
            }
            else if (token.SequenceEqual("draw"))
            {
                _engine.PrintPosition();
            }
            else if (token.SequenceEqual("eval"))
            {
                _engine.PrintEvaluation();
            }
            else if (token.SequenceEqual("bench"))
            {
                RunBenchmark(tokens);
            }
            else
            {
                Console.WriteLine($"Unknown command: {token}");
            }
        }

        private static void SetOption(ReadOnlySpan<char> tokens)
        {
            tokens = tokens.ConsumeTo(' ', out ReadOnlySpan<char> token);

            if (!token.SequenceEqual("name")) { return; }

            int valueIndex = tokens.IndexOf("value", StringComparison.OrdinalIgnoreCase);
            ReadOnlySpan<char> name  = valueIndex == -1 ? tokens  : tokens[..valueIndex].TrimEnd();
            ReadOnlySpan<char> value = valueIndex == -1 ? default : tokens[(valueIndex + "value".Length + 1)..].TrimStart();

            _engine.SetOption(name, value);
        }

        private static void SetPosition(ReadOnlySpan<char> tokens)
        {
            tokens = tokens.ConsumeTo(' ', out ReadOnlySpan<char> token);
            int moveIndex = tokens.IndexOf("moves", StringComparison.OrdinalIgnoreCase);

            ReadOnlySpan<char> fen;
            if (token.SequenceEqual("startpos"))
            {
                fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1".AsSpan();
            }
            else if (token.SequenceEqual("fen"))
            {
                fen = moveIndex == -1 ? tokens : tokens[..moveIndex].TrimEnd();
            }
            else { return; }

            ReadOnlySpan<char> moves = moveIndex == -1 ? default : tokens[(moveIndex + "moves".Length + 1)..].TrimStart();

            _engine.SetPosition(fen, moves);
        }

        private static void StartSearching(ReadOnlySpan<char> tokens)
        {
            UciGoOptions options = new();

            while (!tokens.IsEmpty)
            {
                tokens = tokens.ConsumeTo(' ', out ReadOnlySpan<char> token);

                if (token.SequenceEqual("depth"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int depth)) { options.Depth = depth; }
                }
                else if (token.SequenceEqual("movetime"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int movetime)) { options.MoveTime = movetime; }
                }
                else if (token.SequenceEqual("nodes"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int nodes)) { options.Nodes = nodes; }
                }
                else if (token.SequenceEqual("wtime"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int wtime)) { options.Wtime = wtime; }
                }
                else if (token.SequenceEqual("btime"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int btime)) { options.Btime = btime; }
                }
                else if (token.SequenceEqual("winc"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int winc)) { options.Winc = winc; }
                }
                else if (token.SequenceEqual("binc"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int binc)) { options.Binc = binc; }
                }
                else if (token.SequenceEqual("infinite"))
                {
                    options.IsInfinite = true;
                }
                else if (token.SequenceEqual("ponder"))
                {
                    _engine.StartPondering();
                }
            }

            _engine.Search(options);
        }

        private static void RunBenchmark(ReadOnlySpan<char> tokens)
        {
            string path  = string.Empty;
            int    depth = 1;

            while (!tokens.IsEmpty)
            {
                tokens = tokens.ConsumeTo(' ', out ReadOnlySpan<char> token);

                if (token.SequenceEqual("path"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    path   = token.ToString();
                }
                else if (token.SequenceEqual("depth"))
                {
                    tokens = tokens.ConsumeTo(' ', out token);
                    if (int.TryParse(token, out int d)) { depth = d; }
                }
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Benchmark file was not found.", path);
            }

            _engine.RunBenchmark(File.ReadLines(path), depth);
        }
    }
}
