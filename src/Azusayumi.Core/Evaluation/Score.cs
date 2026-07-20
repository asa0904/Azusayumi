using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Evaluation
{
    internal readonly struct Score
    {
        internal static readonly Score Zero = new(0, 0);

        private readonly int _packed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Score(short mid, short end)
        {
            _packed = (mid << 16) + end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Score(int packed)
        {
            _packed = packed;
        }

        internal readonly int Mid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_packed + 0x8000) >> 16;
        }

        internal readonly int End
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)_packed;
        }

        public static implicit operator Score((short Mid, short End) score)
        {
            return new Score(score.Mid, score.End);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Score operator +(Score left, Score right)
        {
            return new Score(left._packed + right._packed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Score operator -(Score left, Score right)
        {
            return new Score(left._packed - right._packed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Score operator *(int scaler, Score score)
        {
            return new Score(scaler * score._packed);
        }

        public override string ToString()
        {
            return $"({Mid}, {End})";
        }
    }
}
