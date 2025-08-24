namespace Azusayumi.GameLogic
{
    internal static class Bitboard
    {
        public const ulong A1 = 1UL << Square.A1;
        public const ulong B1 = 1UL << Square.B1;
        public const ulong C1 = 1UL << Square.C1;
        public const ulong D1 = 1UL << Square.D1;
        public const ulong E1 = 1UL << Square.E1;
        public const ulong F1 = 1UL << Square.F1;
        public const ulong G1 = 1UL << Square.G1;
        public const ulong H1 = 1UL << Square.H1;
        public const ulong A2 = 1UL << Square.A2;
        public const ulong B2 = 1UL << Square.B2;
        public const ulong C2 = 1UL << Square.C2;
        public const ulong D2 = 1UL << Square.D2;
        public const ulong E2 = 1UL << Square.E2;
        public const ulong F2 = 1UL << Square.F2;
        public const ulong G2 = 1UL << Square.G2;
        public const ulong H2 = 1UL << Square.H2;
        public const ulong A3 = 1UL << Square.A3;
        public const ulong B3 = 1UL << Square.B3;
        public const ulong C3 = 1UL << Square.C3;
        public const ulong D3 = 1UL << Square.D3;
        public const ulong E3 = 1UL << Square.E3;
        public const ulong F3 = 1UL << Square.F3;
        public const ulong G3 = 1UL << Square.G3;
        public const ulong H3 = 1UL << Square.H3;
        public const ulong A4 = 1UL << Square.A4;
        public const ulong B4 = 1UL << Square.B4;
        public const ulong C4 = 1UL << Square.C4;
        public const ulong D4 = 1UL << Square.D4;
        public const ulong E4 = 1UL << Square.E4;
        public const ulong F4 = 1UL << Square.F4;
        public const ulong G4 = 1UL << Square.G4;
        public const ulong H4 = 1UL << Square.H4;
        public const ulong A5 = 1UL << Square.A5;
        public const ulong B5 = 1UL << Square.B5;
        public const ulong C5 = 1UL << Square.C5;
        public const ulong D5 = 1UL << Square.D5;
        public const ulong E5 = 1UL << Square.E5;
        public const ulong F5 = 1UL << Square.F5;
        public const ulong G5 = 1UL << Square.G5;
        public const ulong H5 = 1UL << Square.H5;
        public const ulong A6 = 1UL << Square.A6;
        public const ulong B6 = 1UL << Square.B6;
        public const ulong C6 = 1UL << Square.C6;
        public const ulong D6 = 1UL << Square.D6;
        public const ulong E6 = 1UL << Square.E6;
        public const ulong F6 = 1UL << Square.F6;
        public const ulong G6 = 1UL << Square.G6;
        public const ulong H6 = 1UL << Square.H6;
        public const ulong A7 = 1UL << Square.A7;
        public const ulong B7 = 1UL << Square.B7;
        public const ulong C7 = 1UL << Square.C7;
        public const ulong D7 = 1UL << Square.D7;
        public const ulong E7 = 1UL << Square.E7;
        public const ulong F7 = 1UL << Square.F7;
        public const ulong G7 = 1UL << Square.G7;
        public const ulong H7 = 1UL << Square.H7;
        public const ulong A8 = 1UL << Square.A8;
        public const ulong B8 = 1UL << Square.B8;
        public const ulong C8 = 1UL << Square.C8;
        public const ulong D8 = 1UL << Square.D8;
        public const ulong E8 = 1UL << Square.E8;
        public const ulong F8 = 1UL << Square.F8;
        public const ulong G8 = 1UL << Square.G8;
        public const ulong H8 = 1UL << Square.H8;

        public const ulong Rank1 = 0x00000000000000FF;
        public const ulong Rank2 = Rank1 << 8 * 1;
        public const ulong Rank3 = Rank1 << 8 * 2;
        public const ulong Rank4 = Rank1 << 8 * 3;
        public const ulong Rank5 = Rank1 << 8 * 4;
        public const ulong Rank6 = Rank1 << 8 * 5;
        public const ulong Rank7 = Rank1 << 8 * 6;
        public const ulong Rank8 = Rank1 << 8 * 7;

        public const ulong FileA = 0x0101010101010101;
        public const ulong FileB = FileA << 1;
        public const ulong FileC = FileA << 2;
        public const ulong FileD = FileA << 3;
        public const ulong FileE = FileA << 4;
        public const ulong FileF = FileA << 5;
        public const ulong FileG = FileA << 6;
        public const ulong FileH = FileA << 7;

        public static readonly ulong[][] Lines;
        public static readonly ulong[][] BetweenLines;
        public static readonly ulong[][] SquaresNearKing;

        static Bitboard()
        {
            Lines        = new ulong[64][];
            BetweenLines = new ulong[64][];
            for (byte i = 0; i < 64; i++)
            {
                Lines[i]        = new ulong[64];
                BetweenLines[i] = new ulong[64];

                for (byte j = 0; j < 64; j++)
                {
                    ulong square1 = 1UL << i;
                    ulong square2 = 1UL << j;
                    ulong squares = square1 | square2;

                    if ((Attack.GetBishopAttacks(i, square1) & square2) != 0)
                    {
                        Lines[i][j] = Attack.GetBishopAttacks(i, square1) & Attack.GetBishopAttacks(j, square2) | squares;
                        BetweenLines[i][j] = Attack.GetBishopAttacks(i, squares) & Attack.GetBishopAttacks(j, squares);
                    }

                    if ((Attack.GetRookAttacks(i, square1) & square2) != 0)
                    {
                        Lines[i][j] = Attack.GetRookAttacks(i, square1) & Attack.GetRookAttacks(j, square2) | squares;
                        BetweenLines[i][j] = Attack.GetRookAttacks(i, squares) & Attack.GetRookAttacks(j, squares);
                    }
                }
            }

            SquaresNearKing = [new ulong[64], new ulong[64]];
            for (int i = 0; i < 64; i++)
            {
                SquaresNearKing[Color.White][i] = Attack.KingAttacks[i] | (Attack.KingAttacks[i] << 8);
                SquaresNearKing[Color.Black][i] = Attack.KingAttacks[i] | (Attack.KingAttacks[i] >> 8);
            }
        }

        public static void Print(ulong bitboard)
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    if (file == 0) Console.Write($"{rank + 1} | ");
                    Console.Write((1UL << 8 * rank + file & bitboard) != 0 ? "1 " : ". ");
                    if (file == 7) Console.WriteLine();
                }
            }

            Console.WriteLine("   ----------------\n    a b c d e f g h\n");
        }

        public static byte PopLSB(ref ulong bitboard)
        {
            byte lsbIndex = (byte)System.Numerics.BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            return lsbIndex;
        }
    }
}