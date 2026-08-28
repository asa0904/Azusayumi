using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal class SearchManager
    {
        internal volatile bool IsOver;

        private int  _totalTime;
        private int  _moveTime;
        private long _maxNodes;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        private readonly SearchWorker _worker;

        internal SearchManager()
        {
            _stopwatch = new System.Diagnostics.Stopwatch();
            _worker    = new SearchWorker(this);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyPosition(Board board)
        {
            _worker.CopyPosition(board);
        }

        internal SearchInfo Search<TLogger>(SearchConditions conditions = default)
            where TLogger : struct, ILogger
        {
            _stopwatch.Restart();

            IsOver = false;

            _totalTime = (conditions.Time / 20) + (conditions.Inc / 2);
            _moveTime  = conditions.MoveTime;
            _maxNodes  = conditions.Nodes;

            int maxDepth = conditions.Depth == 0 ? SearchWorker.MaxPly : conditions.Depth;
            SearchInfo result = _worker.IterativeDeepeningSearch<TLogger>(maxDepth);

            _stopwatch.Stop();

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Stop()
        {
            IsOver = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ShouldStop(long nodes)
        {
            if ((nodes & 1023) != 0) { return false; }

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
