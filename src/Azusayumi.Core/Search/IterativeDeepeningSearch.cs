using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal SearchInfo IterativeDeepeningSearch<TLogger>(int maxDepth) where TLogger : struct, ILogger
        {
            _nodes = 0L;
            _highestDepth = 0;

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply: 0));
            if (_board.IsWhiteToMove) { MoveGenerator<White>.GenerateLegalMoves(ref buffer, _board, _board.IsInCheck<White>()); }
            else                      { MoveGenerator<Black>.GenerateLegalMoves(ref buffer, _board, _board.IsInCheck<Black>()); }
            Span<ScoredMove> rootMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(rootMoves, _board);

            SearchInfo info = new();
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                int score = SearchRoot(rootMoves, depth, alpha: -Infinity, beta: Infinity);

                if (_manager.IsOver) { break; }

                info.BestMove   = _pvTable.BestMove;
                info.PonderMove = _pvTable.PonderMove;

                info.Depth        = depth;
                info.HighestDepth = _manager.HighestDepth;
                info.Score        = score;
                info.Nodes        = _manager.NodesSpent;
                info.Time         = _manager.TimeSpent;

                TLogger.Log(info, _pvTable.PV);
            }

            return info;
        }

        private int SearchRoot(Span<ScoredMove> rootMoves, int depth, int alpha, int beta)
        {
            _nodes++;
            _pvTable.Clear(ply: 0);

            bool isWhiteToMove = _board.IsWhiteToMove;
            int  bestValue     = -Infinity;
            bool isInCheck     = isWhiteToMove ? _board.IsInCheck<White>() : _board.IsInCheck<Black>();

            for (int i = 0; i < rootMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, rootMoves);

                _board.MakeMove(move);
                int value = isWhiteToMove ? -AlphaBetaSearch<Black>(depth - 1, ply: 1, -beta, -alpha)
                                          : -AlphaBetaSearch<White>(depth - 1, ply: 1, -beta, -alpha);
                _board.UnmakeMove(move);

                if (_manager.IsOver) { return DrawValue; }

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value >= beta) { break; }

                    alpha = value;
                    _pvTable.Write(ply: 0, move);
                }
            }

            if (rootMoves.Length == 0) { return isInCheck ? -MateValue : DrawValue; }

            return bestValue;
        }
    }
}
