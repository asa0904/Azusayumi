using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal enum NodeType : byte
    {
        PV, Cut, All
    }

    internal struct TTEntry(NodeType nodeType, ulong key, int depth, int value, Move move)
    {
        public NodeType NodeType = nodeType;
        public ulong    Key      = key;
        public int      Depth    = depth;
        public int      Value    = value;
        public Move     Move     = move;

        public override readonly string ToString()
        {
            return $"nodetype {NodeType} depth {Depth} score {Value} bestmove {Move}";
        }
    }

    internal class TranspositionTable
    {
        private readonly ulong lowerBitsMask;
        private readonly TTEntry[] entries;

        public TranspositionTable(int sizeMB)
        {
            int maxSize = sizeMB * 1024 * 1024 / System.Runtime.InteropServices.Marshal.SizeOf(typeof(TTEntry));

            int size = 1;
            while (2 * size <= maxSize) size *= 2;

            lowerBitsMask = (ulong)(size - 1);
            entries = new TTEntry[size];
        }

        public TTEntry Read(Board board)
        {
            return entries[board.State.Key & lowerBitsMask];
        }

        public void Write(NodeType nodeType, Board board, int depth, int value, Move move)
        {
            int index = (int)(board.State.Key & lowerBitsMask);

            if (nodeType == NodeType.PV || depth >= entries[index].Depth)
                entries[index] = new TTEntry(nodeType, board.State.Key, depth, value, move);
        }

        public void Clear()
        {
            for (int i = 0; i < entries.Length; i++) entries[i] = default;
        }
    }
}
