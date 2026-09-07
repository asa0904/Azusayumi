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

            RootMoveBuffer buffer = new(_rootMoves);
            if (isWhiteToMove) { MoveGenerator<White>.GenerateLegalMoves(ref buffer, _board, isInCheck); }
            else               { MoveGenerator<Black>.GenerateLegalMoves(ref buffer, _board, isInCheck); }
            Span<RootMove> rootMoves = buffer.AsSpan();

            if (rootMoves.Length == 0) { return default; }

            for (int depth = 1; !_manager.IsOver && depth <= maxDepth; depth++)
            {
                SearchRoot<TLogger>(rootMoves, depth, alpha: -Infinity, beta: Infinity);
            }

            RootMove bestMove = rootMoves[0];
            return new SearchResult(bestMove.Move, bestMove.PonderMove);
        }

        private void SearchRoot<TLogger>(Span<RootMove> rootMoves, int depth, int alpha, int beta) where TLogger : struct, ILogger
        {
            _nodes++;
            _pvTable.Clear(ply: 0);

            bool isWhiteToMove = _board.IsWhiteToMove;

            int maxPVCount = Math.Min(_manager.Settings.PVCount, rootMoves.Length);
            for (int pvIndex = 0; pvIndex < maxPVCount; pvIndex++)
            {
                int bestValue = alpha = -Infinity;

                for (int i = pvIndex; i < rootMoves.Length; i++)
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
                        bestValue = value;

                        if (value >= beta) { break; }

                        alpha = value;
                        _pvTable.Write(ply: 0, move);
                        rootMoves[i].SavePV(_pvTable.PV);

                        MoveOrdering.InsertBefore(from: i, to: pvIndex, rootMoves);

                        if (_nodes > OutputLimit)
                        {
                            rootMoves[pvIndex].Info = new()
                            {
                                Depth        = depth,
                                HighestDepth = _manager.HighestDepth,
                                Score        = bestValue,
                                Nodes        = _manager.NodesSpent,
                                Time         = _manager.TimeSpent,
                            };
                            for (int j = 0; j < maxPVCount; j++)
                            {
                                rootMoves[j].Info.PVIndex = j + 1;
                                TLogger.LogFullInfo(rootMoves[j].Info, rootMoves[j].PV);
                            }
                        }
                    }
                }

                rootMoves[pvIndex].Info = new()
                {
                    Depth        = depth,
                    HighestDepth = _manager.HighestDepth,
                    PVIndex      = pvIndex + 1,
                    Score        = bestValue,
                    Nodes        = _manager.NodesSpent,
                    Time         = _manager.TimeSpent,
                };
            }

            for (int pvIndex = 0; pvIndex < maxPVCount; pvIndex++)
            {
                TLogger.LogFullInfo(rootMoves[pvIndex].Info, rootMoves[pvIndex].PV);
            }
        }
    }
}
