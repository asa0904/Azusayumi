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

            bool isWhiteToMove = _board.IsWhiteToMove;
            bool isInCheck     = isWhiteToMove ? _board.IsInCheck<White>() : _board.IsInCheck<Black>();

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply: 0));
            if (isWhiteToMove) { MoveGenerator<White>.GenerateLegalMoves(ref buffer, _board, isInCheck); }
            else               { MoveGenerator<Black>.GenerateLegalMoves(ref buffer, _board, isInCheck); }
            Span<ScoredMove> rootMoves = buffer.AsSpan();

            if (rootMoves.Length == 0) { return default; }

            for (int depth = 1; !_manager.IsOver && depth <= maxDepth; depth++)
            {
                SearchRoot<TLogger>(rootMoves, depth, alpha: -Infinity, beta: Infinity);
            }

            return new SearchResult(_pvTable.BestMove, _pvTable.PonderMove);
        }

        private void SearchRoot<TLogger>(Span<ScoredMove> rootMoves, int depth, int alpha, int beta) where TLogger : struct, ILogger
        {
            _nodes++;
            _pvTable.Clear(ply: 0);

            bool isWhiteToMove = _board.IsWhiteToMove;
            int  bestIndex     = 0;
            int  bestValue     = -Infinity;

            for (int i = 0; i < rootMoves.Length; i++)
            {
                Move move = rootMoves[i].Move;

                if (_nodes > OutputLimit) { TLogger.LogCurrentMove(depth, move, i + 1); }

                _board.MakeMove(move);
                int value = isWhiteToMove ? -AlphaBetaSearch<Black>(depth - 1, ply: 1, -beta, -alpha)
                                          : -AlphaBetaSearch<White>(depth - 1, ply: 1, -beta, -alpha);
                _board.UnmakeMove(move);

                if (_manager.IsOver) { return; }

                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;

                    if (value >= beta) { break; }

                    alpha = value;
                    _pvTable.Write(ply: 0, move);

                    if (_nodes > OutputLimit)
                    {
                        SearchInfo info = new()
                        {
                            Depth        = depth,
                            HighestDepth = _manager.HighestDepth,
                            Score        = bestValue,
                            Nodes        = _manager.NodesSpent,
                            Time         = _manager.TimeSpent,
                        };
                        TLogger.LogFullInfo(info, _pvTable.PV);
                    }
                }
            }

            MoveOrdering.InsertTop(bestIndex, rootMoves);

            SearchInfo result = new()
            {
                Depth        = depth,
                HighestDepth = _manager.HighestDepth,
                Score        = bestValue,
                Nodes        = _manager.NodesSpent,
                Time         = _manager.TimeSpent,
            };
            TLogger.LogFullInfo(result, _pvTable.PV);
        }
    }
}
