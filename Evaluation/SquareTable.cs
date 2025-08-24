using Azusayumi.GameLogic;

namespace Azusayumi.Evaluation
{
    internal class SquareTable
    {
        public readonly int[][] Values;

        public SquareTable(int[] squareTable)
        {
            Values = [new int[64], new int[64]];
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                Values[Color.White][squareIndex] = squareTable[squareIndex];
                Values[Color.Black][squareIndex] = -squareTable[squareIndex ^ 56];
            }
        }

        public SquareTable(int material, int[] squareTable)
        {
            Values = [new int[64], new int[64]];
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                Values[Color.White][squareIndex] =  material + squareTable[squareIndex];
                Values[Color.Black][squareIndex] = -material - squareTable[squareIndex ^ 56];
            }
        }
    }
}
