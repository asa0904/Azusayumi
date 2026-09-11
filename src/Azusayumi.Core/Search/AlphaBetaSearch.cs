using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        private int AlphaBetaSearch<TColor>(int depth, int ply, int alpha, int beta) where TColor : struct, IColor
        {
            if ((_nodes & 1023) == 0 && _manager.ShouldStop()) { return DrawValue; }

            if (depth == 0)
            {
#if COLLECT_STATS
                _statistics.HorizonNodes++;
#endif
                return Quiesce<TColor>(ply, alpha, beta);
            }

            _nodes++;
            _pvTable.Clear(ply);

#if COLLECT_STATS
            _statistics.InteriorNodes++;
#endif

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            int  bestValue = -Infinity;
            bool isInCheck = _board.IsInCheck<TColor>();

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            MoveGenerator<TColor>.GenerateLegalMoves(ref buffer, _board, isInCheck);
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.Score<TColor>(scoredMoves, _killerTable[ply, 0], _killerTable[ply, 1], _historyTable, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = -OppositeAlphaBetaSearch<TColor>(depth - 1, ply + 1, -beta, -alpha);
                _board.UnmakeMove<TColor>(move);

                if (_manager.IsOver) { return DrawValue; }

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value > alpha)
                    {
                        if (value >= beta)
                        {
#if COLLECT_STATS
                            _statistics.CutNodes++;
                            if (i == 0) { _statistics.FirstCutNodes++; }
#endif

                            if (_board.IsQuiet(move))
                            {
                                _killerTable.Write(move, ply);
                                _historyTable.Update<TColor>(move.Key, bonus: depth * depth);
                            }

                            break;
                        }

                        alpha = value;
                        _pvTable.Write(ply, move);
                    }
                }
            }

            if (scoredMoves.Length == 0) { return isInCheck ? -MateValue + ply : DrawValue; }

            return bestValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int OppositeAlphaBetaSearch<TColor>(int depth, int ply, int alpha, int beta)
            where TColor : struct, IColor
        {
            return TColor.IsWhite ? AlphaBetaSearch<Black>(depth, ply, alpha, beta)
                                  : AlphaBetaSearch<White>(depth, ply, alpha, beta);
        }
    }
}
