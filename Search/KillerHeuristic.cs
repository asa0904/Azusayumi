using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal class KillerHeuristic
    {
        private readonly Move[][] killerMoves;

        public KillerHeuristic(int maxPly)
        {
            killerMoves = new Move[maxPly][];
            for (int i = 0; i < killerMoves.Length; i++) killerMoves[i] = [Move.Null, Move.Null];
        }

        public void Write(Move killerMove, int ply)
        {
            if (killerMoves[ply][0] != killerMove)
            {
                killerMoves[ply][1] = killerMoves[ply][0];
                killerMoves[ply][0] = killerMove;
            }
        }

        public Move Read(int ply, int slot)
        {
            return killerMoves[ply][slot];
        }

        public void Clear()
        {
            for (int i = 0; i < killerMoves.Length; i++) killerMoves[i] = [Move.Null, Move.Null];
        }
    }
}
