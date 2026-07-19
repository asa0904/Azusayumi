using System.Numerics;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal static class Bitboard
    {
        internal const ulong A1 = 1UL << 00, B1 = 1UL << 01, C1 = 1UL << 02, D1 = 1UL << 03, E1 = 1UL << 04, F1 = 1UL << 05, G1 = 1UL << 06, H1 = 1UL << 07;
        internal const ulong A2 = 1UL << 08, B2 = 1UL << 09, C2 = 1UL << 10, D2 = 1UL << 11, E2 = 1UL << 12, F2 = 1UL << 13, G2 = 1UL << 14, H2 = 1UL << 15;
        internal const ulong A3 = 1UL << 16, B3 = 1UL << 17, C3 = 1UL << 18, D3 = 1UL << 19, E3 = 1UL << 20, F3 = 1UL << 21, G3 = 1UL << 22, H3 = 1UL << 23;
        internal const ulong A4 = 1UL << 24, B4 = 1UL << 25, C4 = 1UL << 26, D4 = 1UL << 27, E4 = 1UL << 28, F4 = 1UL << 29, G4 = 1UL << 30, H4 = 1UL << 31;
        internal const ulong A5 = 1UL << 32, B5 = 1UL << 33, C5 = 1UL << 34, D5 = 1UL << 35, E5 = 1UL << 36, F5 = 1UL << 37, G5 = 1UL << 38, H5 = 1UL << 39;
        internal const ulong A6 = 1UL << 40, B6 = 1UL << 41, C6 = 1UL << 42, D6 = 1UL << 43, E6 = 1UL << 44, F6 = 1UL << 45, G6 = 1UL << 46, H6 = 1UL << 47;
        internal const ulong A7 = 1UL << 48, B7 = 1UL << 49, C7 = 1UL << 50, D7 = 1UL << 51, E7 = 1UL << 52, F7 = 1UL << 53, G7 = 1UL << 54, H7 = 1UL << 55;
        internal const ulong A8 = 1UL << 56, B8 = 1UL << 57, C8 = 1UL << 58, D8 = 1UL << 59, E8 = 1UL << 60, F8 = 1UL << 61, G8 = 1UL << 62, H8 = 1UL << 63;

        internal const ulong Rank1 = 0x00000000000000FF;
        internal const ulong Rank2 = Rank1 << (8 * 1);
        internal const ulong Rank3 = Rank1 << (8 * 2);
        internal const ulong Rank4 = Rank1 << (8 * 3);
        internal const ulong Rank5 = Rank1 << (8 * 4);
        internal const ulong Rank6 = Rank1 << (8 * 5);
        internal const ulong Rank7 = Rank1 << (8 * 6);
        internal const ulong Rank8 = Rank1 << (8 * 7);

        internal const ulong FileA = 0x0101010101010101;
        internal const ulong FileB = FileA << 1;
        internal const ulong FileC = FileA << 2;
        internal const ulong FileD = FileA << 3;
        internal const ulong FileE = FileA << 4;
        internal const ulong FileF = FileA << 5;
        internal const ulong FileG = FileA << 6;
        internal const ulong FileH = FileA << 7;

        private static readonly ulong[] _betweenTable = GenerateBetweenTable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetLsb(ulong bitboard)
        {
            return BitOperations.TrailingZeroCount(bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PopLsb(ref ulong bitboard)
        {
            int lsbIndex = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            return lsbIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PopCount(ulong bitboard)
        {
            return BitOperations.PopCount(bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetBetweenSquares(int included, int excluded)
        {
            return _betweenTable[(included << 6) | excluded];
        }

        private static ulong[] GenerateBetweenTable()
        {
            static ulong CalculateRay(int squareIndex, (int X, int Y) direction, ulong occupancy)
            {
                int rank = Square.GetRank(squareIndex);
                int file = Square.GetFile(squareIndex);
                occupancy ^= 1UL << Square.GetIndex(rank, file);

                ulong ray = 0UL;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int r = rank, f = file;
                    while (0 <= r && r < 8 && 0 <= f && f < 8)
                    {
                        ray |= 1UL << Square.GetIndex(r, f);
                        if ((ray & occupancy) != 0) { break; }

                        r += sign * direction.X;
                        f += sign * direction.Y;
                    }
                }

                return ray;
            }

            (int, int) virtical = (0, 1), holizontal = (1, 0), diagonal = (1, 1), antiDiagonal = (-1, 1);
            ulong[] table = new ulong[Square.Length * Square.Length];
            for (int included = 0; included < Square.Length; included++)
            {
                for (int excluded = 0; excluded < Square.Length; excluded++)
                {
                    table[(included << 6) | excluded] = 1UL << included;

                    if (included == excluded) { continue; }

                    ulong occpancy = (1UL << included) | (1UL << excluded);

                    ulong line = CalculateRay(included, virtical, occpancy) & CalculateRay(excluded, virtical, occpancy);
                    if (line != 0) { table[(included << 6) | excluded] = line ^ (1UL << excluded); continue; }

                    line = CalculateRay(included, holizontal, occpancy) & CalculateRay(excluded, holizontal, occpancy);
                    if (line != 0) { table[(included << 6) | excluded] = line ^ (1UL << excluded); continue; }

                    line = CalculateRay(included, diagonal, occpancy) & CalculateRay(excluded, diagonal, occpancy);
                    if (line != 0) { table[(included << 6) | excluded] = line ^ (1UL << excluded); continue; }

                    line = CalculateRay(included, antiDiagonal, occpancy) & CalculateRay(excluded, antiDiagonal, occpancy);
                    if (line != 0) { table[(included << 6) | excluded] = line ^ (1UL << excluded); continue; }
                }
            }

            return table;
        }
    }
}
