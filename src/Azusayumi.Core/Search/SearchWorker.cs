using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal const int MaxPly = 64;

        private const int Infinity  = short.MaxValue;
        private const int MateValue = 10000;
        private const int DrawValue = 0;

        private readonly Board _board = new();

        private ref struct MoveBuffer : IMoveBuffer
        {
            private readonly Span<ScoredMove> _buffer;
            private int _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal MoveBuffer(Span<ScoredMove> buffer)
            {
                _buffer = buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Move move)
            {
                _buffer[_count++].Move = move;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly Span<ScoredMove> AsSpan()
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
}
