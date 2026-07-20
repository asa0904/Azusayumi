using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Evaluation
{
    internal class PieceSquareTables
    {
        private static readonly Score[] _whiteScores = GenerateWhiteScores();
        private static readonly Score[] _blackScores = GenerateBlackScores(_whiteScores);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Score GetScore<TColor>(int pieceType, int squareIndex) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whiteScores[(pieceType << 6) | squareIndex] : _blackScores[(pieceType << 6) | squareIndex];
        }

        private static Score[] GenerateWhiteScores()
        {
            // Initialize with random values until parameter tuning is implemented.
            Random random = new(2006_09_04);
            Score[] whiteScores = new Score[PieceType.Length * Square.Length];
            for (int i = 0; i < whiteScores.Length; i++)
            {
                short mid = (short)random.Next(-1000, 1000);
                short end = (short)random.Next(-1000, 1000);
                whiteScores[i] = new Score(mid, end);
            }

            return whiteScores;
        }

        private static Score[] GenerateBlackScores(Score[] whiteScores)
        {
            Score[] blackScores = new Score[PieceType.Length * Square.Length];
            for (int pieceType = PieceType.Pawn; pieceType < PieceType.Length; pieceType++)
            {
                for (int squareIndex = 0; squareIndex < Square.Length; squareIndex++)
                {
                    blackScores[(pieceType << 6) | squareIndex] -= whiteScores[(pieceType << 6) | (squareIndex ^ 56)];
                }
            }

            return blackScores;
        }
    }
}
