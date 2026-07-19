using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal static class Color
    {
        internal const int White = 0, Black = 1, Length = 2;
    }

    internal static class PieceType
    {
        internal const int Pawn = 0, Knight = 1, Bishop = 2, Rook = 3, Queen = 4, King = 5, None = 6, Length = 6;
    }

    internal static class Square
    {
        internal const int A1 = 00, B1 = 01, C1 = 02, D1 = 03, E1 = 04, F1 = 05, G1 = 06, H1 = 07;
        internal const int A2 = 08, B2 = 09, C2 = 10, D2 = 11, E2 = 12, F2 = 13, G2 = 14, H2 = 15;
        internal const int A3 = 16, B3 = 17, C3 = 18, D3 = 19, E3 = 20, F3 = 21, G3 = 22, H3 = 23;
        internal const int A4 = 24, B4 = 25, C4 = 26, D4 = 27, E4 = 28, F4 = 29, G4 = 30, H4 = 31;
        internal const int A5 = 32, B5 = 33, C5 = 34, D5 = 35, E5 = 36, F5 = 37, G5 = 38, H5 = 39;
        internal const int A6 = 40, B6 = 41, C6 = 42, D6 = 43, E6 = 44, F6 = 45, G6 = 46, H6 = 47;
        internal const int A7 = 48, B7 = 49, C7 = 50, D7 = 51, E7 = 52, F7 = 53, G7 = 54, H7 = 55;
        internal const int A8 = 56, B8 = 57, C8 = 58, D8 = 59, E8 = 60, F8 = 61, G8 = 62, H8 = 63;
        internal const int None = 64, Length = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetRank(int squareIndex)
        {
            return squareIndex >> 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetFile(int squareIndex)
        {
            return squareIndex & 7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetIndex(int rank, int file)
        {
            return (rank << 3) | file;
        }

        internal static string ToCoordinate(int squareIndex)
        {
            char file = (char)('a' + GetFile(squareIndex));
            char rank = (char)('1' + GetRank(squareIndex));

            return $"{file}{rank}";
        }
    }

    internal static class Castling
    {
        internal const int WhiteO_O   = 1;
        internal const int WhiteO_O_O = 1 << 1;
        internal const int BlackO_O   = 1 << 2;
        internal const int BlackO_O_O = 1 << 3;
        internal const int Length     = 1 << 4;

        private static readonly int[] _lostRightsTable = GenerateLostRightsTable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetLostRights(int squareIndex)
        {
            return _lostRightsTable[squareIndex];
        }

        private static int[] GenerateLostRightsTable()
        {
            int[] table = new int[Square.Length];

            table[Square.A1] = WhiteO_O_O;
            table[Square.E1] = WhiteO_O | WhiteO_O_O;
            table[Square.H1] = WhiteO_O;

            table[Square.A8] = BlackO_O_O;
            table[Square.E8] = BlackO_O | BlackO_O_O;
            table[Square.H8] = BlackO_O;

            return table;
        }
    }
}
