using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal interface IMoveBuffer
    {
        internal void Add(Move move);
    }

    internal ref struct MoveBuffer(Span<Move> buffer) : IMoveBuffer
    {
        private readonly Span<Move> _buffer = buffer;
        private int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Move move)
        {
            _buffer[_count++] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Span<Move> AsSpan()
        {
            return _buffer[.._count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            _count = 0;
        }
    }
}
