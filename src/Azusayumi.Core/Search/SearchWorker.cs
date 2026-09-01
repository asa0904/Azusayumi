using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker(SearchManager manager)
    {
        internal const int MaxPly    = 64;
        internal const int Infinity  = short.MaxValue;
        internal const int MateValue = 10000;
        internal const int DrawValue = 0;

        private long _nodes;

        private readonly SearchManager _manager       = manager;
        private readonly Board         _board         = new();
        private readonly MoveArrayPool _moveArrayPool = new();
        private readonly PVTable       _pvTable       = new();

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

        private class MoveArrayPool
        {
            private const int MaxLength = 256;
            private readonly ScoredMove[] _moves = new ScoredMove[MaxLength * MaxPly];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Span<ScoredMove> GetSpan(int ply)
            {
                return _moves.AsSpan().Slice(ply * MaxLength, MaxLength);
            }
        }

        internal long NodesSpent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _nodes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyPosition(Board board)
        {
            _board.CopyFrom(board);
        }
    }
}
