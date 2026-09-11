using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal int QuiescePV<TColor>(int ply, int alpha, int beta) where TColor : struct, IColor
        {
            _nodes++;
            _pvTable.Clear(ply);

#if COLLECT_STATS
            _statistics.QuiescentNodes++;
#endif

            if (ply > _highestDepth) { _highestDepth = ply; }

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            bool isInCheck = _board.IsInCheck<TColor>();
            int  standPat  = isInCheck ? -Infinity : Evaluator.Evaluate<TColor>(_board);
            int  bestValue = standPat;

            if (standPat >= beta) { return standPat; }

            if (standPat > alpha) { alpha = standPat; }

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            if (isInCheck) { MoveGenerator<TColor>.GenerateEvasionMoves(ref buffer, _board); }
            else           { MoveGenerator<TColor>.GenerateTacticalMoves(ref buffer, _board); }
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(scoredMoves, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = TColor.IsWhite ? -QuiescePV<Black>(ply + 1, -beta, -alpha) : -QuiescePV<White>(ply + 1, -beta, -alpha);
                _board.UnmakeMove<TColor>(move);

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value > alpha)
                    {
                        if (value >= beta)
                        {
#if COLLECT_STATS
                            _statistics.QCutNodes++;
                            if (i == 0) { _statistics.FirstQCutNodes++; }
#endif
                            break;
                        }

                        alpha = value;
                        _pvTable.Write(ply, move);
                    }
                }
            }

            if (isInCheck && scoredMoves.Length == 0) { return -MateValue + ply; }

            return bestValue;
        }

        private int QuiesceNonPV<TColor>(int ply, int beta) where TColor : struct, IColor
        {
            _nodes++;

#if COLLECT_STATS
            _statistics.QuiescentNodes++;
#endif

            if (ply > _highestDepth) { _highestDepth = ply; }

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            bool isInCheck = _board.IsInCheck<TColor>();
            int  standPat  = isInCheck ? -Infinity : Evaluator.Evaluate<TColor>(_board);
            int  bestValue = standPat;

            if (standPat >= beta) { return standPat; }

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            if (isInCheck) { MoveGenerator<TColor>.GenerateEvasionMoves(ref buffer, _board); }
            else           { MoveGenerator<TColor>.GenerateTacticalMoves(ref buffer, _board); }
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.ScoreCaptures(scoredMoves, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = TColor.IsWhite ? -QuiesceNonPV<Black>(ply + 1, -(beta - 1)) : -QuiesceNonPV<White>(ply + 1, -(beta - 1));
                _board.UnmakeMove<TColor>(move);

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value >= beta)
                    {
#if COLLECT_STATS
                        _statistics.QCutNodes++;
                        if (i == 0) { _statistics.FirstQCutNodes++; }
#endif
                        break;
                    }
                }
            }

            if (isInCheck && scoredMoves.Length == 0) { return -MateValue + ply; }

            return bestValue;
        }
    }
}
