using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal const int MaxPly    = 64;
        internal const int Infinity  = short.MaxValue;
        internal const int MateValue = 10000;
        internal const int DrawValue = 0;

        private long _nodes;
        private int  _highestDepth;

        private readonly SearchManager _manager;
        private readonly Board         _board;
        private readonly MoveArrayPool _moveArrayPool;
        private readonly PVTable       _pvTable;
        private readonly RootMove[]    _rootMoves;

        internal SearchWorker(SearchManager manager)
        {
            _manager       = manager;
            _board         = new Board();
            _moveArrayPool = new MoveArrayPool();
            _pvTable       = new PVTable();
            _rootMoves     = new RootMove[256];
            
            for (int i = 0; i < _rootMoves.Length; i++)
            {
                _rootMoves[i] = new RootMove();
            }
        }

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

        internal int HighestDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _highestDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyPosition(Board board)
        {
            _board.CopyFrom(board);
        }
    }
}
