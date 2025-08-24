using Azusayumi.GameLogic;

namespace Azusayumi.Evaluation
{
    internal class PawnHashTable
    {
        public const int Null = int.MinValue;

        private readonly ulong lowerBitsMask;
        private readonly Entry[] entries;

        private struct Entry(ulong key, int value)
        {
            public ulong Key   = key;
            public int   Value = value;
        }

        public PawnHashTable(int sizeKB)
        {
            int maxSize = sizeKB * 1024 / System.Runtime.InteropServices.Marshal.SizeOf(typeof(Entry));

            int size = 1;
            while (2 * size <= maxSize) size *= 2;

            lowerBitsMask = (ulong)(size - 1);
            entries = new Entry[size];
        }

        public int Read(Board board)
        {
            Entry entry = entries[board.State.PawnKey & lowerBitsMask];
            
            if (board.State.PawnKey != entry.Key) return Null;

            return entry.Value;
        }

        public void Write(Board board, int value)
        {
            entries[board.State.PawnKey & lowerBitsMask] = new Entry(board.State.PawnKey, value);
        }

        public void Clear()
        {
            for (int i = 0; i < entries.Length; i++) entries[i] = default;
        }
    }
}
