using Azusayumi.Core.GameLogic;
using Azusayumi.Core.Search;
using System.Reflection;

namespace Azusayumi.Cli
{
    internal class UciEngine
    {
        private bool _isSearching;
        private bool _exitEngine;
        private SearchConditions _conditions;
        private readonly Board                _board;
        private readonly SearchSettings       _settings;
        private readonly SearchManager        _searchManager;
        private readonly ManualResetEventSlim _searchStartEvent;
        private readonly ManualResetEventSlim _searchFinishEvent;

        private record struct GoCommand(int Depth, int MoveTime, int Nodes, int TotalTime, int Inc);

        internal UciEngine()
        {
            _board             = new Board();
            _settings          = new SearchSettings();
            _searchManager     = new SearchManager(_settings);
            _searchStartEvent  = new ManualResetEventSlim(initialState: false);
            _searchFinishEvent = new ManualResetEventSlim(initialState: true);

            Thread searchThread = new(SearchLoop)
            {
                IsBackground = true,
                Name = "SearchThread",
                Priority = ThreadPriority.Normal,
            };
            searchThread.Start();
        }

        internal static void PrintUciInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Console.WriteLine($"id name Azusayumi {version}");
            Console.WriteLine("id author Asato Kamamoto");
            Console.WriteLine("option name Ponder type check default false");
            Console.WriteLine("option name MultiPV type spin default 1 min 1 max 256");
        }

        internal void Clear()
        {
            Stop();
            _board.Set("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            _searchManager.CopyPosition(_board);
        }

        internal void SetOption(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            if (name.SequenceEqual("Ponder") && bool.TryParse(value, out bool ponderEnabled))
            {
                _settings.PonderEnabled = ponderEnabled;
            }
            else if (name.SequenceEqual("MultiPV") && int.TryParse(value, out int multiPV))
            {
                _settings.PVCount = multiPV;
            }
            else
            {
                Console.WriteLine("Unknown option.");
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
            _conditions = new SearchConditions()
            {
                Depth      = options.Depth,
                Time       = _board.IsWhiteToMove ? options.Wtime : options.Btime,
                Inc        = _board.IsWhiteToMove ? options.Winc  : options.Binc,
                MoveTime   = options.MoveTime,
                Nodes      = options.Nodes,
                IsInfinite = options.IsInfinite,
            };

            _isSearching = true;
            _searchStartEvent.Set();
            _searchFinishEvent.Reset();
        }

        internal void Stop()
        {
            _isSearching = false;
            _searchManager.Stop();
            _searchFinishEvent.Wait();
            _searchStartEvent.Reset();
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
            _exitEngine = true;
            _isSearching = false;
            _searchManager.Stop();
            _searchFinishEvent.Wait();
            _searchStartEvent.Set();
        }

        internal void PrintPosition()
        {
            _board.Print();
        }

        private void SearchLoop()
        {
            Span<char> answer = new char[32];
            "bestmove ".CopyTo(answer);

            while (!_exitEngine)
            {
                _searchStartEvent.Wait();

                if (_exitEngine) { return; }
                if (!_isSearching) { continue; }

                SearchResult result = _searchManager.Search<UciLogger>(_conditions);

                int offset = "bestmove ".Length;
                result.BestMove.Format(answer[offset..], out int written);
                offset += written;
                " ponder ".CopyTo(answer[offset..]);
                offset += " ponder ".Length;
                result.PonderMove.Format(answer[offset..], out written);
                offset += written;

                Console.WriteLine(answer[..offset]);

                _searchStartEvent.Reset();
                _searchFinishEvent.Set();
            }
        }
    }
}
