namespace Azusayumi.GameLogic
{
    internal static class Color
    {
        public const byte White = 0, Black = 1;
    }

    internal static class PieceType
    {
        public const byte Pawn = 0, Knight = 1, Bishop = 2, Rook = 3, Queen = 4, King = 5, None = 6;
    }

    internal static class MoveType
    {
        public const byte Quiet = 0, Promotion = 1, Castling = 2, EnPassant = 3;
    }

    internal static class Castling
    {
        public const int WhiteKingCastle  = 1;
        public const int WhiteQueenCastle = 1 << 1;
        public const int BlackKingCastle  = 1 << 2;
        public const int BlackQueenCastle = 1 << 3;

        public static readonly Move WhiteKingMove  = new Move(Square.E1, Square.G1).MakeCastling();
        public static readonly Move WhiteQueenMove = new Move(Square.E1, Square.C1).MakeCastling();
        public static readonly Move BlackKingMove  = new Move(Square.E8, Square.G8).MakeCastling();
        public static readonly Move BlackQueenMove = new Move(Square.E8, Square.C8).MakeCastling();

        public static readonly ulong[] Path =
        [
            0x0000000000000000,
            0x0000000000000060,
            0x000000000000000E,
            0x0000000000000000,
            0x6000000000000000,
            0x0000000000000000,
            0x0000000000000000,
            0x0000000000000000,
            0x0E00000000000000,
        ];
        public static readonly ulong[] PassedSquares =
        [
            0x0000000000000000,
            0x0000000000000070,
            0x000000000000001C,
            0x0000000000000000,
            0x7000000000000000,
            0x0000000000000000,
            0x0000000000000000,
            0x0000000000000000,
            0x1C00000000000000,
        ];
        public static readonly byte[]  RightMasks =
        [
            0b1101, 0b1111, 0b1111, 0b1111, 0b1100, 0b1111, 0b1111, 0b1110,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111, 0b1111,
            0b0111, 0b1111, 0b1111, 0b1111, 0b0011, 0b1111, 0b1111, 0b1011,
        ];
    }
}