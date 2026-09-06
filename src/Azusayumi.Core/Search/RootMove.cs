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

    internal ref struct RootMoveBuffer : IMoveBuffer
    {
        private readonly Span<RootMove> _buffer;
        private int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RootMoveBuffer(Span<RootMove> buffer)
        {
            _buffer = buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Move move)
        {
            _buffer[_count++].Move = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Span<RootMove> AsSpan()
        {
            return _buffer[.._count];
        }
    }
}
