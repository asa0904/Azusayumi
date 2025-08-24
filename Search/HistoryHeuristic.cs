using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal class HistoryHeuristic
    {
        private readonly int[][][] histories;

        public HistoryHeuristic()
        {
            histories = new int[2][][];
            for (int side = Color.White; side <= Color.Black; side++)
            {
                histories[side] = new int[64][];
                for (int i = 0; i < 64; i++) histories[side][i] = new int[64];
            }
        }

        public void Update(byte side, Move move, int bonus)
        {
            histories[side][move.OriginIndex][move.TargetIndex] += bonus;
        }

        public int Read(byte side, Move move)
        {
            return histories[side][move.OriginIndex][move.TargetIndex];
        }

        public void Clear()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    histories[Color.White][i][j] = 0;
                    histories[Color.Black][i][j] = 0;
                }
            }
        }
    }
}
