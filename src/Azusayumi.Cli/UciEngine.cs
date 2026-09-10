using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;
using Azusayumi.Core.Search;
using System.Reflection;

namespace Azusayumi.Cli
{
    internal class UciEngine
    {
        private readonly Board          _board;
        private readonly SearchSettings _settings;
        private readonly SearchManager  _searchManager;
        private readonly TraceContext   _traceContext;

        internal UciEngine()
        {
            _board         = new Board();
            _settings      = new SearchSettings();
            _searchManager = new SearchManager(_settings);
            _traceContext  = new TraceContext();

            _searchManager.Start<UciLogger>();
        }

        internal static void PrintUciInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Console.WriteLine($"id name Azusayumi {version}");
            Console.WriteLine("id author Asato Kamamoto");
            Console.WriteLine("option name Clear Hash type button");
            Console.WriteLine("option name Ponder type check default false");
            Console.WriteLine("option name MultiPV type spin default 1 min 1 max 256");
        }

        internal void Clear()
        {
            Stop();
            _board.Set("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            _searchManager.CopyPosition(_board);
            _searchManager.ClearHash();
        }

        internal void SetOption(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            if (name.SequenceEqual("Clear Hash"))
            {
                _searchManager.ClearHash();
            }
            else if (name.SequenceEqual("Ponder"))
            {
                if (bool.TryParse(value, out bool ponderEnabled))
                {
                    _settings.PonderEnabled = ponderEnabled;
                }
                else
                {
                    Console.WriteLine("Ponder value must be either 'true' or 'false'.");
                }
            }
            else if (name.SequenceEqual("MultiPV"))
            {
                if (int.TryParse(value, out int multiPV) && (multiPV is >= 1 and <= 256))
                {
                    _settings.PVCount = multiPV;
                }
                else
                {
                    Console.WriteLine("MultiPV value must be between 1 and 256.");
                }
            }
            else
            {
                Console.WriteLine($"Unknown option: {name}");
            }
        }

        internal void SetPosition(ReadOnlySpan<char> fen, ReadOnlySpan<char> uciMoves)
        {
            _board.Set(fen);

            MoveBuffer buffer = new(stackalloc Move[256]);
            while (!uciMoves.IsEmpty)
            {
                Move move = Move.Null;
                uciMoves = uciMoves.ConsumeTo(' ', out ReadOnlySpan<char> uciMove);

                buffer.Clear();
                MoveGenerator.GenerateLegalMoves(ref buffer, _board);
                Span<Move> legalMoves = buffer.AsSpan();

                for (int i = 0; i < legalMoves.Length; i++)
                {
                    if (legalMoves[i].Equals(uciMove)) { move = legalMoves[i]; break; }
                }

                if (move != Move.Null)
                {
                    _board.MakeMove(move);
                }
                else
                {
                    Console.WriteLine($"Invalid move sequence: '{uciMove}' is illegal. Ignoring subsequent moves.");
                    break;
                }
            }

            _searchManager.CopyPosition(_board);
        }

        internal void Search(UciGoOptions options)
        {
            _searchManager.StartSearch(new SearchConditions()
            {
                Depth      = options.Depth,
                Time       = _board.IsWhiteToMove ? options.Wtime : options.Btime,
                Inc        = _board.IsWhiteToMove ? options.Winc  : options.Binc,
                MoveTime   = options.MoveTime,
                Nodes      = options.Nodes,
                IsInfinite = options.IsInfinite,
            });
        }

        internal void Stop()
        {
            _searchManager.Stop();
        }

        internal void StartPondering()
        {
            _searchManager.StartPondering();
        }

        internal void StopPondering()
        {
            _searchManager.StopPondering();
        }

        internal void Quit()
        {
            _searchManager.Quit();
        }

        internal void PrintPosition()
        {
            _board.Print();
        }

        internal void PrintEvaluation()
        {
            _traceContext.Clear();
            _ = Evaluator<TraceContext>.EvaluatePst(_board, _traceContext);
            _ = Evaluator<TraceContext>.Evaluate(_board, _traceContext);

            double phase = 100.0 * _board.Phase / GamePhase.Max;
            int evaluation = Evaluator.Evaluate(_board);
            Console.WriteLine($"Total evaluation: {evaluation / 100.0:0.00} (MG: {phase:0}%, EG: {100.0 - phase:0}%)");
            _traceContext.Print();
        }

        internal void RunBenchmark(IEnumerable<string> fens, int depth)
        {
            int count = fens.Count();

            long totalNodes = 0L;
            long totalTime  = 0L;

            int i = 1;
            foreach (string fen in fens)
            {
                Console.WriteLine($"Position: {i++}/{count} ({fen})");
                
                _board.Set(fen);
                _searchManager.CopyPosition(_board);

                (long nodes, long time) = _searchManager.RunBenchmark(depth);
                totalNodes += nodes;
                totalTime  += time;
            }

            Console.WriteLine("-----------------------------------");
            Console.WriteLine($"Nodes : {totalNodes:N0}");
            Console.WriteLine($"Time  : {totalTime:N0} ms");
            Console.WriteLine($"NPS   : {totalNodes / totalTime:N0} kn/s\n");
        }
    }
}
