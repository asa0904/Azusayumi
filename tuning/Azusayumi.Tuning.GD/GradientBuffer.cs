using Azusayumi.Core.Evaluation;

namespace Azusayumi.Tuning.GD
{
    internal class GradientBuffer
    {
        private readonly Score[][] _gradients;

        internal GradientBuffer(Weights weights)
        {
            _gradients = new Score[(int)Term.Length][];
            for (Term term = 0; term < Term.Length; term++)
            {
                _gradients[(int)term] = new Score[weights.GetLength(term)];
            }
        }

        internal Score this[Term term, int index]
        {
            get => _gradients[(int)term][index];
            set => _gradients[(int)term][index] = value;
        }

        internal void Clear()
        {
            for (int term = 0; term < _gradients.Length; term++)
            {
                for (int i = 0; i < _gradients[term].Length; i++)
                {
                    _gradients[term][i] = Score.Zero;
                }
            }
        }

        internal void MergeFrom(GradientBuffer other)
        {
            for (int term = 0; term < _gradients.Length; term++)
            {
                for (int i = 0; i < _gradients[term].Length; i++)
                {
                    _gradients[term][i] += other._gradients[term][i];
                    other._gradients[term][i] = Score.Zero;
                }
            }
        }
    }
}
