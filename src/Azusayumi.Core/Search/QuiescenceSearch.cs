using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal int Quiesce<TColor>(int ply, int alpha, int beta) where TColor : struct, IColor
        {
            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            bool isInCheck = _board.IsInCheck<TColor>();
            int  standPat  = isInCheck ? -Infinity : Evaluator.Evaluate<TColor>(_board);
            int  bestValue = standPat;

            if (standPat >= beta) { return standPat; }

            if (standPat > alpha) { alpha = standPat; }

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            if (isInCheck) { MoveGenerator<TColor>.GenerateEvasionMoves(ref buffer, _board); }
            else           { MoveGenerator<TColor>.GenerateTactialMoves(ref buffer, _board); }
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(scoredMoves, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = TColor.IsWhite ? -Quiesce<Black>(ply + 1, -beta, -alpha) : -Quiesce<White>(ply + 1, -beta, -alpha);
                _board.UnmakeMove<TColor>(move);

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value > alpha)
                    {
                        if (value >= beta) { break; }

                        alpha = value;
                    }
                }
            }

            if (isInCheck && scoredMoves.Length == 0) { return -MateValue + ply; }

            return bestValue;
        }
    }
}
