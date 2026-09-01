using Azusayumi.Core.Evaluation;

namespace Azusayumi.Tuning.GD.Tests
{
    public class GradientCalculationTests
    {
        [Theory]
        [MemberData(nameof(Fens.GetTestPositions), MemberType = typeof(Fens))]
        public void AccumulateGradient_MatchesNumericalDifferentiation(string fen)
        {
            Weights weights = new();
            SetRandomly(weights);

            Model          model     = new(weights);
            GradientBuffer analytics = new(weights);
            GradientBuffer numerics  = new(weights);

            float result = new Random(2006_09_04).Next(3) / 2.0F;
            DatasetEntry entry = new(fen, result);

            model.AccumulateGradients(entry, analytics);
            CalculateGradientNumerically(entry, weights, numerics);

            const double AbsMax = 1e-8;
            for (Term term = 0; term < Term.Length; term++)
            {
                for (int index = 0; index < weights.GetLength(term); index++)
                {
                    Score analytic = analytics[term, index];
                    Score numeric  = numerics[term, index];
                    Score diff     = analytic - numeric;

                    double absErrorMG = Math.Abs(diff.Mid);
                    Assert.True(absErrorMG <= AbsMax,
                        $"Term: {term}, Index: {index}, Mid Abs Error: {absErrorMG} > {AbsMax}\n" +
                        $"(analytic={analytic.Mid}, numeric={numeric.Mid})\n" +
                        $"FEN: {fen}");

                    double absErrorEG = Math.Abs(diff.End);
                    Assert.True(absErrorEG <= AbsMax,
                        $"Term: {term}, Index: {index}, End Abs Error: {absErrorEG} > {AbsMax}\n" +
                        $"(analytic={analytic.End}, numeric={numeric.End})\n" +
                        $"FEN: {fen}");
                }
            }
        }

        private static void SetRandomly(Weights weights)
        {
            Random random = new(2006_09_04);
            for (Term term = 0; term < Term.Length; term++)
            {
                for (int i = 0; i < weights.GetLength(term); i++)
                {
                    double mid = 100 * (random.NextDouble() - 0.5);
                    double end = 100 * (random.NextDouble() - 0.5);
                    weights[term, i] = new Score(mid, end);
                }
            }
        }

        private static void CalculateGradientNumerically(DatasetEntry entry, Weights weights, GradientBuffer gradients)
        {
            const double Epsilon = 1e-7;
            Score epsilonMG = (Epsilon, 0.0);
            Score epsilonEG = (0.0, Epsilon);

            Model model = new(weights);
            for (Term term = 0; term < Term.Length; term++)
            {
                for (int index = 0; index < weights.GetLength(term); index++)
                {
                    Score origin = weights[term, index];

                    // MG
                    weights[term, index] = origin + epsilonMG;
                    double pred1  = model.Predict(entry.Fen);
                    double error1 = pred1 - entry.Result;

                    weights[term, index] = origin - epsilonMG;
                    double pred2  = model.Predict(entry.Fen);
                    double error2 = pred2 - entry.Result;

                    // EG
                    weights[term, index] = origin + epsilonEG;
                    double pred3  = model.Predict(entry.Fen);
                    double error3 = pred3 - entry.Result;

                    weights[term, index] = origin - epsilonEG;
                    double pred4  = model.Predict(entry.Fen);
                    double error4 = pred4 - entry.Result;

                    double gradientMG = ((error1 * error1) - (error2 * error2)) / (2 * Epsilon);
                    double gradientEG = ((error3 * error3) - (error4 * error4)) / (2 * Epsilon);
                    gradients[term, index] = (gradientMG, gradientEG);

                    weights[term, index] = origin;
                }
            }
        }
    }
}
