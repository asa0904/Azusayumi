using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker : IDisposable
    {
        internal const int MaxPly    = 64;
        internal const int Infinity  = short.MaxValue;
        internal const int MateValue = 10000;
        internal const int DrawValue = 0;

        private long _nodes;
        private int  _highestDepth;

        private readonly SearchManager _manager;
        private readonly Board         _board;
        private readonly MoveArrayPool _moveArrayPool;
        private readonly PVTable       _pvTable;
        private readonly RootMove[]    _rootMoves;

        private bool _exitEngine;
        private Thread? _searchThread;
        private readonly ManualResetEventSlim _startSignal;

#if COLLECT_STATS
        private readonly SearchStatistics _statistics = new();
#endif

        internal SearchWorker(SearchManager manager)
        {
            _manager       = manager;
            _board         = new Board();
            _moveArrayPool = new MoveArrayPool();
            _pvTable       = new PVTable();
            _rootMoves     = new RootMove[256];
            _startSignal   = new ManualResetEventSlim(initialState: false);
            
            for (int i = 0; i < _rootMoves.Length; i++)
            {
                _rootMoves[i] = new RootMove();
            }
        }

        private ref struct MoveBuffer : IMoveBuffer
        {
            private readonly Span<ScoredMove> _buffer;
            private int _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal MoveBuffer(Span<ScoredMove> buffer)
            {
                _buffer = buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Move move)
            {
                _buffer[_count++].Move = move;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly Span<ScoredMove> AsSpan()
            {
                return _buffer[.._count];
            }
        }

        private class MoveArrayPool
        {
            private const int MaxLength = 256;
            private readonly ScoredMove[] _moves = new ScoredMove[MaxLength * MaxPly];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Span<ScoredMove> GetSpan(int ply)
            {
                return _moves.AsSpan().Slice(ply * MaxLength, MaxLength);
            }
        }

        internal long NodesSpent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _nodes;
        }

        internal int HighestDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _highestDepth;
        }

        internal void Start<TLogger>() where TLogger : struct, ILogger
        {
            if (_searchThread is not null) { return; }

            _searchThread = new Thread(SearchLoop<TLogger>);
            _searchThread.Start();
        }

        public void Dispose()
        {
            if (!_exitEngine)
            {
                _exitEngine = true;
                StartSearch();

                if (_searchThread is not null
                 && _searchThread.IsAlive)
                {
                    _searchThread.Join(1000);
                }

                _startSignal.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearHash()
        {
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyPosition(Board board)
        {
            _board.CopyFrom(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void StartSearch()
        {
            _startSignal.Set();
        }

        private void SearchLoop<TLogger>() where TLogger : struct, ILogger
        {
            while (!_exitEngine)
            {
                _startSignal.Wait();

                if (_exitEngine) { break; }

                SearchResult result = default;
                try
                {
                    result = IterativeDeepeningSearch<TLogger>();
                    _manager.WaitForStopSignal();
                }
                catch (Exception ex)
                {
                    TLogger.LogException(ex);
                }
                finally
                {
                    TLogger.LogBestMove(result);
                    _startSignal.Reset();
                }
            }
        }
    }
}
