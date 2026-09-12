using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal enum NodeType : byte
    {
        PV, Cut, All
    }

    internal record struct TTEntry(ulong Key, int Value, Move Move, ushort Age, NodeType NodeType, int Depth)
    {
        internal const int Size = 16;
    }

    internal class TranspositionTable
    {
        private ushort _generation;
        private readonly ulong _lowerBitsMask;
        private readonly TTEntry[] _entries;

        internal TranspositionTable(int sizeMB)
        {
            ulong maxSize = (ulong)sizeMB * 1024 * 1024 / TTEntry.Size;

            ulong size = 1;
            while (2 * size <= maxSize) { size *= 2; }

            _lowerBitsMask = size - 1;
            _entries = new TTEntry[size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRead(ulong key, out TTEntry entry)
        {
            entry = _entries[key & _lowerBitsMask];
            return entry.Key == key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(ulong key, int value, Move move, NodeType nodeType, int depth)
        {
            int index = (int)(key & _lowerBitsMask);
            if (nodeType == NodeType.PV
             || depth >= _entries[index].Depth - (_generation - _entries[index].Age))
            {
                _entries[index] = new TTEntry(key, value, move, _generation, nodeType, depth);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Age()
        {
            _generation += 4;
        }

        internal void Clear()
        {
            for (int i = 0; i < _entries.Length; i++) { _entries[i] = default; }
        }
    }
}
