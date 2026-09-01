using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Tuning.GD
{
    internal class Model(Weights weights, double k = 1.13)
    {
        private readonly Board _board = new();
        private readonly TuningContext _context = new(weights);

        private readonly double _k = k;

        private static readonly double Log10 = Math.Log(10.0);

        internal double Predict(string fen)
        {
            _board.Set(fen);
            _context.Clear();

            return Sigmoid(Evaluator.Evaluate(_board, _context));
        }

        internal double AccumulateGradients(DatasetEntry entry, GradientBuffer gradients)
        {
            double prediction = Predict(entry.Fen);
            double error      = prediction - entry.Result;
            double phase      = (double)_board.GetPhase() / GamePhase.Max;
            Score  gradient   = new Score(phase, 1.0 - phase) * 2.0 * error * (_k / 400.0 * Log10) * prediction * (1.0 - prediction);
            
            _context.AccumulateGradients(gradient, gradients);

            return error * error;
        }

        private double Sigmoid(double value)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, -_k * value / 400.0));
        }
    }
}
