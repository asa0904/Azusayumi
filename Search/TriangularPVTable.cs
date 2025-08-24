using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal class TriangularPVTable
    {
        private readonly int    maxLength;
        private readonly int[]  lengths;
        private readonly Move[] moves;

        public Move BestMove => moves[0];
        
        public Move PonderMove => moves[1];

        public TriangularPVTable(int maxPly)
        {
            maxLength = maxPly;
            lengths   = new int[maxPly];
            
            moves = new Move[GetIndex(maxPly)];
            for (int i = 0; i < moves.Length; i++) moves[i] = Move.Null;
        }

        public override string ToString()
        {
            string[] png = new string[lengths[0]];
            for (int i = 0; i < png.Length; i++) png[i] = moves[i].ToString();

            return string.Join(" ", png);
        }

        public void Write(int ply, Move move)
        {
            lengths[ply] = lengths[ply + 1] + 1;

            int targetIndex = GetIndex(ply);
            int sourceIndex = targetIndex + (maxLength - ply);

            moves[targetIndex++] = move;
            for (int i = 0; i < lengths[ply]; i++) moves[targetIndex++] = moves[sourceIndex++];
        }

        public Move Read(int ply)
        {
            return moves[GetIndex(ply)];
        }

        public void Clear(int ply)
        {
            int startIndex = GetIndex(ply);
            int endIndex   = startIndex + lengths[ply];

            for (int i = startIndex; i < endIndex; i++) moves[i] = Move.Null;

            lengths[ply] = 0;
        }

        private int GetIndex(int ply)
        {
            return ply * (2 * maxLength + 1 - ply) / 2;
        }
    }
}
