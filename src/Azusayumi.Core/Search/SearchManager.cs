using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    public class SearchManager
    {
        internal volatile bool IsOver;
        internal volatile bool IsPondering;

        internal readonly SearchSettings Settings;

        private int  _totalTime;
        private int  _moveTime;
        private long _maxNodes;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        private readonly SearchWorker         _worker;
        private readonly ManualResetEventSlim _stopSignal;

        public SearchManager(SearchSettings settings)
        {
            Settings = settings;

            _stopwatch  = new System.Diagnostics.Stopwatch();
            _worker     = new SearchWorker(this);
            _stopSignal = new ManualResetEventSlim(initialState: false);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyPosition(Board board)
        {
            _worker.CopyPosition(board);
        }

        public SearchResult Search<TLogger>(SearchConditions conditions = default)
            where TLogger : struct, ILogger
        {
            _stopwatch.Restart();

            _stopSignal.Reset();

            IsOver = false;

            _totalTime = (conditions.Time / 20) + (conditions.Inc / 2);
            _moveTime  = conditions.MoveTime;
            _maxNodes  = conditions.Nodes;

            int maxDepth = conditions.Depth == 0 ? SearchWorker.MaxPly : conditions.Depth;
            SearchResult result = _worker.IterativeDeepeningSearch<TLogger>(maxDepth);

            if (IsPondering || conditions.IsInfinite) { _stopSignal.Wait(); }

            _stopwatch.Stop();

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            IsOver      = true;
            IsPondering = false;
            _stopSignal.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartPondering()
        {
            IsPondering = Settings.PonderEnabled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopPondering()
        {
            IsPondering = false;
            _stopSignal.Set();
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

        public int GetQuiescentScore(Board board)
        {
            const int Infinity = SearchWorker.Infinity;

            CopyPosition(board);
            return board.IsWhiteToMove ? +_worker.Quiesce<White>(ply: 0, alpha: -Infinity, beta: Infinity)
                                       : -_worker.Quiesce<Black>(ply: 0, alpha: -Infinity, beta: Infinity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ShouldStop()
        {
            if (IsPondering) { return false; }

            long time = TimeSpent;
            if ((_totalTime > 0 && time >= _totalTime)
             || (_moveTime > 0  && time >= _moveTime)
             || (_maxNodes > 0  && NodesSpent >= _maxNodes))
            {
                IsOver = true;
                return true;
            }

            return false;
        }
    }
}
