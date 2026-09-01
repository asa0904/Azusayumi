using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Tuning.GD.Tests
{
    public class EvaluatorTests
    {
        [Theory]
        [MemberData(nameof(Fens.GetTestPositions), MemberType = typeof(Fens))]
        public void Evaluate_ReturnsSameValueAsSearchEvaluation(string fen)
        {
            Weights weights = new();
            SetSearchWeights(weights);
            TuningContext context = new(weights);

            Board board = new(historyCapacity: 2);
            board.Set(fen);
            
            double expected = Core.Evaluation.Evaluator.Evaluate(board);
            double actual   = Evaluator.Evaluate(board, context);

            Assert.True(Math.Abs(expected - actual) < 2.0);
        }

        private static void SetSearchWeights(Weights weights)
        {
            for (Term term = 0; term < Term.Length; term++)
            {
                for (int i = 0; i < weights.GetLength(term); i++)
                {
                    (int Mid, int End) = Core.Evaluation.Weights.GetValue(term, i);
                    weights[term, i] = new Score(Mid, End);
                }
            }
        }
    }
}
