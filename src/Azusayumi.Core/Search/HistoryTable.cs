using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal class HistoryTable
    {
        internal const int Max = 1 << Shift;

        private const int Shift = 14;
        private readonly short[] _whiteHistories = new short[Square.Length * Square.Length];
        private readonly short[] _blackHistories = new short[Square.Length * Square.Length];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update<TColor>(int key, int bonus) where TColor : struct, IColor
        {
            int clampedBonus = Math.Clamp(bonus, -Max, Max);
            if (TColor.IsWhite)
            {
                _whiteHistories[key] += (short)(clampedBonus - ((_whiteHistories[key] * Math.Abs(clampedBonus)) >> Shift));
            }
            else
            {
                _blackHistories[key] += (short)(clampedBonus - ((_blackHistories[key] * Math.Abs(clampedBonus)) >> Shift));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal short Read<TColor>(int key) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whiteHistories[key] : _blackHistories[key];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Age()
        {
            for (int i = 0; i < _whiteHistories.Length; i++)
            {
                _whiteHistories[i] >>= 3;
            }
            for (int i = 0; i < _blackHistories.Length; i++)
            {
                _blackHistories[i] >>= 3;
            }
        }

        internal void Clear()
        {
            for (int i = 0; i < _whiteHistories.Length; i++)
            {
                _whiteHistories[i] = 0;
            }
            for (int i = 0; i < _blackHistories.Length; i++)
            {
                _blackHistories[i] = 0;
            }
        }
    }
}
