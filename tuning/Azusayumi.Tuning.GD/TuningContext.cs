using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Tuning.GD
{
    internal struct TuningContext(Weights weights) : IEvaluationContext
    {
        private readonly record struct Feature(Term Term, int Index, int Count);

        private class Buffer
        {
            private readonly Feature[] _features = new Feature[256];
            private int _count;

            internal ReadOnlySpan<Feature> Features => _features.AsSpan()[.._count];

            internal void Add(Term term, int index, int count)
            {
                _features[_count++] = new Feature(term, index, count);
            }

            internal void Clear()
            {
                _count = 0;
            }
        }

        private readonly Weights _weights = weights;
        private readonly Buffer  _buffer  = new();

        internal Score Score { get; private set; }

        public void Add<TColor>(Term term, int index = 0, int count = 1) where TColor : struct, IColor
        {
            if (!TColor.IsWhite) { count = -count; }
            _buffer.Add(term, index, count);
            Score += count * _weights[term, index];
        }

        internal readonly void Clear()
        {
            _buffer.Clear();
        }

        internal readonly void AccumulateGradients(Score gradient, GradientBuffer gradients)
        {
            for (int i = 0; i < _buffer.Features.Length; i++)
            {
                Feature feature = _buffer.Features[i];
                gradients[feature.Term, feature.Index] += feature.Count * gradient;
            }
        }
    }
}
