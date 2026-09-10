using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal class KillerTable
    {
        private readonly Move[] _killers = new Move[2 * SearchWorker.MaxPly];

        internal Move this[int ply, int slot]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _killers[(ply << 1) | slot];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(Move move, int ply)
        {
            int index = ply << 1;
            if (_killers[index] != move)
            {
                _killers[index + 1] = _killers[index];
                _killers[index] = move;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            for (int i = 0; i < _killers.Length; i++)
            {
                _killers[i] = Move.Null;
            }
        }
    }
}
