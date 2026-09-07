using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal class PVTable
    {
        private readonly int[]  _lengths = new int[SearchWorker.MaxPly + 1];
        private readonly Move[] _moves   = new Move[(SearchWorker.MaxPly * SearchWorker.MaxPly) + 1];

        internal ReadOnlySpan<Move> PV
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _moves.AsSpan()[.._lengths[0]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(int ply, Move move)
        {
            int length = _lengths[ply + 1] + 1;
            _lengths[ply] = length;

            int targetIndex = ply * SearchWorker.MaxPly;
            int sourceIndex = targetIndex + SearchWorker.MaxPly;
            _moves[targetIndex++] = move;
            Span<Move>         target = _moves.AsSpan().Slice(targetIndex, length);
            ReadOnlySpan<Move> source = _moves.AsSpan().Slice(sourceIndex, length);
            source.CopyTo(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear(int ply)
        {
            _lengths[ply] = 0;
        }
    }
}
