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
            Score[] whiteScores = new Score[PieceType.Length * Square.Length];
            for (int pieceType = PieceType.Pawn; pieceType < PieceType.Length; pieceType++)
            {
                Score material = pieceType == PieceType.King ? Score.Zero : Weights.Material[pieceType];
                Score[] pst = pieceType switch
                {
                    PieceType.Pawn   => Weights.PawnPst,
                    PieceType.Knight => Weights.KnightPst,
                    PieceType.Bishop => Weights.BishopPst,
                    PieceType.Rook   => Weights.RookPst,
                    PieceType.Queen  => Weights.QueenPst,
                    _                => Weights.KingPst,
                };
                for (int squareIndex = 0; squareIndex < Square.Length; squareIndex++)
                {
                    whiteScores[(pieceType << 6) | squareIndex] = material + pst[squareIndex ^ 56];
                }
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
