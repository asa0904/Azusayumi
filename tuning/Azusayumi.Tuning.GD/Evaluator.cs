using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Tuning.GD
{
    internal static class Evaluator
    {
        internal static double Evaluate(Board board, TuningContext context)
        {
            context = Evaluator<TuningContext>.EvaluatePst(board, context);
            context = Evaluator<TuningContext>.Evaluate(board, context);

            Score  score  = context.Score;
            double phase  = (double)board.GetPhase() / GamePhase.Max;
            double result = (score.Mid * phase) + (score.End * (1.0 - phase));

            return result;
        }
    }
}
