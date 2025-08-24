namespace Azusayumi.GameLogic
{
    internal static class GamePhase
    {
        public static readonly byte[] Weights = [0, 1, 1, 2, 4, 0];

        public static byte Calculate(byte color, Board board)
        {
            byte phase = 0;
            ulong pieces = board.PiecesByColor[color];

            while (pieces > 0)
            {
                byte squareIndex = Bitboard.PopLSB(ref pieces);
                byte pieceType = board.PieceTypes[squareIndex];

                phase += Weights[pieceType];
            }

            return phase;
        }
    }
}
