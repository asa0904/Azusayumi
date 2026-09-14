using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;
using Azusayumi.Core.Search;

namespace Azusayumi.DatasetBuilder
{
    internal static class QuiescenceSearch
    {
        private const int Infinity = SearchWorker.Infinity;

        internal static int GetScore(Board board)
        {
            return board.IsWhiteToMove ? +Quiesce<White>(board, -Infinity, Infinity)
                                       : -Quiesce<Black>(board, -Infinity, Infinity);
        }

        private static int Quiesce<TColor>(Board board, int alpha, int beta) where TColor : struct, IColor
        {
            bool isInCheck = board.IsInCheck<TColor>();
            int  standPat  = isInCheck ? -Infinity : Evaluator.Evaluate<TColor>(board);
            int  bestValue = standPat;

            if (standPat >= beta) { return standPat; }

            if (standPat > alpha) { alpha = standPat; }

            MoveBuffer buffer = new(stackalloc Move[32]);
            if (isInCheck) { MoveGenerator<TColor>.GenerateEvasionMoves(ref buffer, board); }
            else           { MoveGenerator<TColor>.GenerateTacticalMoves(ref buffer, board); }
            Span<Move> moves = buffer.AsSpan();

            Span<ScoredMove> scoredMoves = stackalloc ScoredMove[moves.Length];
            for (int i = 0; i < scoredMoves.Length; i++)
            {
                scoredMoves[i].Move = moves[i];
            }
            MoveOrdering.ScoreCaptures(scoredMoves, board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                board.MakeMove<TColor>(move);
                int value = TColor.IsWhite ? -Quiesce<Black>(board, -beta, -alpha) : -Quiesce<White>(board, -beta, -alpha);
                board.UnmakeMove<TColor>(move);

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

            if (isInCheck && scoredMoves.Length == 0) { return -Infinity; }

            return bestValue;
        }
    }
}
