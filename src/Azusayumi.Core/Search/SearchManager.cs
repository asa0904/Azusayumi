using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    public class SearchManager
    {
        internal readonly SearchSettings Settings;

        private volatile bool _isOver;
        private volatile bool _isInfinite;

        private int  _maxDepth;
        private int  _totalTime;
        private int  _moveTime;
        private long _maxNodes;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        private TranspositionTable _transpositionTable;

        private readonly SearchWorker         _worker;
        private readonly ManualResetEventSlim _stopSignal;

        public SearchManager(SearchSettings settings)
        {
            Settings = settings;

            _stopwatch          = new System.Diagnostics.Stopwatch();
            _transpositionTable = new(sizeMB: 32);
            _worker             = new SearchWorker(this, _transpositionTable);
            _stopSignal         = new ManualResetEventSlim(initialState: false);
        }

        internal bool IsOver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isOver;
        }

        internal long TimeSpent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stopwatch.ElapsedMilliseconds;
        }

        internal long NodesSpent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _worker.NodesSpent;
        }

        internal int HighestDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _worker.HighestDepth;
        }

        internal int HashUsagePermille
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transpositionTable.GetHashUsagePermille();
        }

        public void Start<TLogger>() where TLogger : struct, ILogger
        {
            _worker.Start<TLogger>();
        }

        public void SetHash(int sizeMB)
        {
            Stop();
            _transpositionTable = new TranspositionTable(sizeMB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearHash()
        {
            _worker.ClearHash();
            _transpositionTable.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyPosition(Board board)
        {
            _worker.CopyPosition(board);
        }

        public void StartSearch(SearchConditions conditions)
        {
            _stopwatch.Restart();
            _stopSignal.Reset();

            _isOver = false;
            _isInfinite |= conditions.IsInfinite;

            _maxDepth  = conditions.Depth == 0 ? SearchWorker.MaxPly : conditions.Depth;
            _totalTime = (conditions.Time / 20) + (conditions.Inc / 2);
            _moveTime  = conditions.MoveTime;
            _maxNodes  = conditions.Nodes;

            _transpositionTable.Age();

            _worker.StartSearch();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            _isOver     = true;
            _isInfinite = false;
            _stopSignal.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartPondering()
        {
            _isInfinite = Settings.PonderEnabled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopPondering()
        {
            _isInfinite = false;
            _stopSignal.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Quit()
        {
            Stop();
            _worker.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMateScore(int score)
        {
            return Math.Abs(score) >= SearchWorker.MateValue - SearchWorker.MaxPly;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMateDistance(int mateScore)
        {
            int ply = mateScore > 0 ? (SearchWorker.MateValue - mateScore + 1) : (-SearchWorker.MateValue - mateScore);
            return ply / 2;
        }

        public (long Nodes, long Time) RunBenchmark(int depth)
        {
            _isOver   = false;
            _maxDepth = depth;

            _stopwatch.Restart();
            _ = _worker.IterativeDeepeningSearch<NullLogger>();
            _stopwatch.Stop();

            return (NodesSpent, TimeSpent);
        }

        public int GetQuiescentScore(Board board)
        {
            const int Infinity = SearchWorker.Infinity;

            CopyPosition(board);
            return board.IsWhiteToMove ? +_worker.QuiescePV<White>(ply: 0, alpha: -Infinity, beta: Infinity)
                                       : -_worker.QuiescePV<Black>(ply: 0, alpha: -Infinity, beta: Infinity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ShouldExitIteration(int depth)
        {
            if (_isOver || depth > _maxDepth)
            {
                _stopwatch.Stop();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ShouldStop()
        {
            if (_isInfinite) { return false; }

            long time = TimeSpent;
            if ((_totalTime > 0 && time >= _totalTime)
             || (_moveTime > 0  && time >= _moveTime)
             || (_maxNodes > 0  && NodesSpent >= _maxNodes))
            {
                _isOver = true;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WaitForStopSignal()
        {
            if (_isInfinite) { _stopSignal.Wait(); }
        }
    }
}
