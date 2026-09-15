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

        private SearchWorker[] _workers;
        private readonly ManualResetEventSlim _stopSignal;

        public SearchManager(SearchSettings settings)
        {
            Settings = settings;

            _stopwatch          = new System.Diagnostics.Stopwatch();
            _transpositionTable = new(sizeMB: 32);
            _stopSignal         = new ManualResetEventSlim(initialState: false);

            _workers = new SearchWorker[1];
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new SearchWorker(threadId: i, manager: this);
            }
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
            get
            {
                long totalNodes = 0L;
                for (int i = 0; i < _workers.Length; i++)
                {
                    totalNodes += _workers[i].NodesSpent;
                }

                return totalNodes;
            }
        }

        internal int HighestDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int highestDepth = 0;
                for (int i = 0; i < _workers.Length; i++)
                {
                    highestDepth = Math.Max(highestDepth, _workers[i].HighestDepth);
                }

                return highestDepth;
            }
        }

        internal TranspositionTable TranspositionTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transpositionTable;
        }

        public void Start<TLogger>() where TLogger : struct, ILogger
        {
            _workers[0].Start<TLogger>();
            for (int i = 1; i < _workers.Length; i++)
            {
                _workers[i].Start<NullLogger>();
            }
        }

        public void SetHash(int sizeMB)
        {
            Stop();
            _transpositionTable = new TranspositionTable(sizeMB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearHash()
        {
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].ClearHash();
            }

            _transpositionTable.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyPosition(Board board)
        {
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].CopyPosition(board);
            }
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

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].ResetCounters();
            }
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].StartSearch();
            }
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
            
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].Dispose();
            }
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
            _ = _workers[0].IterativeDeepeningSearch<NullLogger>();
            _stopwatch.Stop();

            return (NodesSpent, TimeSpent);
        }

        public TTEntry GetTTEntry(Board board)
        {
            return _transpositionTable.TryRead(board.Key, out TTEntry entry) ? entry : default;
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
        internal void WaitForSearchComplete()
        {
            if (_isInfinite) { _stopSignal.Wait(); }

            if (!IsAnySearching()) { return; }

            int count = 0;
            SpinWait spinWait = new();
            do
            {
                spinWait.SpinOnce();
                count++;
            }
            while (IsAnySearching());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAnySearching()
        {
            for (int i = 0; i < _workers.Length; i++)
            {
                if (_workers[i].IsSearching) { return true; }
            }

            return false;
        }
    }
}
