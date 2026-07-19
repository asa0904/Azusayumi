using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Evaluation
{
    internal static class GamePhase
    {
        internal const int Max = 24;

        private static readonly int[] _weights = [0, 1, 1, 2, 4, 0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetWeight(int pieceType)
        {
            return _weights[pieceType];
        }
    }
}
