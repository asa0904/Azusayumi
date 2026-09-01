using Azusayumi.Core.Evaluation;

namespace Azusayumi.Tuning.GD
{
    internal class Optimizer
    {
        private int _step;
        private readonly Score[][] _momentams;
        private readonly Score[][] _velocities;

        internal Optimizer(Weights weights)
        {
            _momentams  = new Score[(int)Term.Length][];
            _velocities = new Score[(int)Term.Length][];
            for (Term term = 0; term < Term.Length; term++)
            {
                _momentams [(int)term] = new Score[weights.GetLength(term)];
                _velocities[(int)term] = new Score[weights.GetLength(term)];
            }
        }

        internal void Update(double learningRate, int dataCount, Weights weights, GradientBuffer gradients)
        {
            const double Beta1 = 0.9;
            const double Beta2 = 0.999;

            _step++;

            for (Term term = 0; term < Term.Length; term++)
            {
                for (int i = 0; i < _velocities[(int)term].Length; i++)
                {
                    Score gradient = gradients[term, i] / dataCount;

                    _momentams[(int)term][i] *= Beta1;
                    _momentams[(int)term][i] += (1.0 - Beta1) * gradient;

                    _velocities[(int)term][i] *= Beta2;
                    _velocities[(int)term][i] += (1.0 - Beta2) * (gradient * gradient);

                    Score momentam = _momentams [(int)term][i] / (1.0 - Math.Pow(Beta1, _step));
                    Score velocity = _velocities[(int)term][i] / (1.0 - Math.Pow(Beta2, _step));

                    double mid = learningRate * momentam.Mid / (Math.Sqrt(velocity.Mid) + 1e-8);
                    double end = learningRate * momentam.End / (Math.Sqrt(velocity.End) + 1e-8);
                    weights[term, i] -= (mid, end);
                }
            }
        }
    }
}
