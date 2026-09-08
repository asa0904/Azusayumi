using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Evaluation
{
    public static class Evaluator
    {
        private struct SearchContext : IEvaluationContext
        {
            private Score _score;

            internal readonly Score Score
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _score;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<TColor>(Term term, int index = 0, int count = 1) where TColor : struct, IColor
            {
                if (TColor.IsWhite) { _score += GetWeight(term, index, count); }
                else                { _score -= GetWeight(term, index, count); }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Score GetWeight(Term term, int index, int count)
            {
                return term switch
                {
                    Term.KnightMobility => Weights.KnightMobility[index],
                    Term.BishopMobility => Weights.BishopMobility[index],
                    Term.RookMobility   => Weights.RookMobility[index],
                    Term.QueenMobility  => Weights.QueenMobility[index],
                    _ => Score.Zero
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Evaluate(Board board)
        {
            SearchContext context = Evaluator<SearchContext>.Evaluate(board, default);
            Score score = board.Score + context.Score;

            return score.Interpolate(board.Phase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Evaluate<TColor>(Board board) where TColor : struct, IColor
        {
            return TColor.IsWhite ? Evaluate(board) : -Evaluate(board);
        }
    }
}
