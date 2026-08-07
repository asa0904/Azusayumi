using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        private int AlphaBetaSearch<TColor>(int depth, int ply, int alpha, int beta) where TColor : struct, IColor
        {
            if (depth == 0) { return Quiesce<TColor>(ply, alpha, beta); }

            _pvTable.Clear(ply);

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            int  bestValue = -Infinity;
            bool isInCheck = _board.IsInCheck<TColor>();

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            MoveGenerator<TColor>.GenerateLegalMoves(ref buffer, _board, isInCheck);
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(scoredMoves, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = -OppositeAlphaBetaSearch<TColor>(depth - 1, ply + 1, -beta, -alpha);
                _board.UnmakeMove<TColor>(move);

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value > alpha)
                    {
                        if (value >= beta) { break; }

                        alpha = value;
                        _pvTable.Write(ply, move);
                    }
                }
            }

            if (scoredMoves.Length == 0) { return _board.IsInCheck<TColor>() ? -MateValue + ply : DrawValue; }

            return bestValue;
        }

        private int OppositeAlphaBetaSearch<TColor>(int depth, int ply, int alpha, int beta)
            where TColor : struct, IColor
        {
            return TColor.IsWhite ? AlphaBetaSearch<Black>(depth, ply, alpha, beta)
                                  : AlphaBetaSearch<White>(depth, ply, alpha, beta);
        }
    }
}
