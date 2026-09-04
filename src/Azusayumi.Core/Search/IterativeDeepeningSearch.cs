using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        private const long OutputLimit = 10_000_000;

        internal SearchResult IterativeDeepeningSearch<TLogger>(int maxDepth) where TLogger : struct, ILogger
        {
            _nodes = 0L;
            _highestDepth = 0;

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply: 0));
            if (_board.IsWhiteToMove) { MoveGenerator<White>.GenerateLegalMoves(ref buffer, _board, _board.IsInCheck<White>()); }
            else                      { MoveGenerator<Black>.GenerateLegalMoves(ref buffer, _board, _board.IsInCheck<Black>()); }
            Span<ScoredMove> rootMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(rootMoves, _board);
            MoveOrdering.SortRootMoves(rootMoves);

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                int score = SearchRoot<TLogger>(rootMoves, depth, alpha: -Infinity, beta: Infinity);

                if (_manager.IsOver) { break; }

                SearchInfo info = new()
                {
                    Depth        = depth,
                    HighestDepth = _manager.HighestDepth,
                    Score        = score,
                    Nodes        = _manager.NodesSpent,
                    Time         = _manager.TimeSpent,
                };
                TLogger.LogFullInfo(info, _pvTable.PV);
            }

            return new SearchResult(_pvTable.BestMove, _pvTable.PonderMove);
        }

        private int SearchRoot<TLogger>(Span<ScoredMove> rootMoves, int depth, int alpha, int beta) where TLogger : struct, ILogger
        {
            _nodes++;
            _pvTable.Clear(ply: 0);

            bool isWhiteToMove = _board.IsWhiteToMove;
            int  bestIndex     = 0;
            int  bestValue     = -Infinity;
            bool isInCheck     = isWhiteToMove ? _board.IsInCheck<White>() : _board.IsInCheck<Black>();

            for (int i = 0; i < rootMoves.Length; i++)
            {
                Move move = rootMoves[i].Move;

                if (_nodes > OutputLimit) { TLogger.LogCurrentMove(depth, move, i + 1); }

                _board.MakeMove(move);
                int value = isWhiteToMove ? -AlphaBetaSearch<Black>(depth - 1, ply: 1, -beta, -alpha)
                                          : -AlphaBetaSearch<White>(depth - 1, ply: 1, -beta, -alpha);
                _board.UnmakeMove(move);

                if (_manager.IsOver) { return DrawValue; }

                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;

                    if (value >= beta) { break; }

                    alpha = value;
                    _pvTable.Write(ply: 0, move);
                }
            }

            if (rootMoves.Length == 0) { return isInCheck ? -MateValue : DrawValue; }

            MoveOrdering.InsertTop(bestIndex, rootMoves);

            return bestValue;
        }
    }
}
