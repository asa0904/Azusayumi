using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal class RootMove
    {
        internal Move       Move;
        internal SearchInfo Info;

        private int _pvLength;
        private readonly Move[] _pv = new Move[SearchWorker.MaxPly];

        internal ReadOnlySpan<Move> PV
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pv.AsSpan(0, _pvLength);
        }

        internal Move PonderMove
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pvLength > 1 ? _pv[1] : Move.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SavePV(ReadOnlySpan<Move> pv)
        {
            _pvLength = pv.Length;
            pv.CopyTo(_pv);
        }
    }
}
