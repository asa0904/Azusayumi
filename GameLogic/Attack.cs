namespace Azusayumi.GameLogic
{
    internal static class Attack
    {
        public static readonly ulong[][] PawnAttacks;
        public static readonly ulong[]   KnightAttacks;
        public static readonly ulong[]   KingAttacks;

        private static readonly MagicEntry[] BishopMagics;
        private static readonly MagicEntry[] RookMagics;
        private static readonly ulong[]      SlidingAttacks;

        private readonly struct MagicEntry(ulong mask, ulong magic, uint offset)
        {
            public readonly ulong Mask   = mask;
            public readonly ulong Magic  = magic;
            public readonly uint  Offset = offset;
        }

        static Attack()
        {
            // Leaping Piece
            PawnAttacks   = [new ulong[64], new ulong[64]];
            KnightAttacks = new ulong[64];
            KingAttacks   = new ulong[64];

            for (int i = 0; i < 64; i++)
            {
                ulong square = 1UL << i;

                PawnAttacks[Color.White][i] = (square << 9) & ~Bitboard.FileA | (square << 7) & ~Bitboard.FileH;
                PawnAttacks[Color.Black][i] = (square >> 9) & ~Bitboard.FileH | (square >> 7) & ~Bitboard.FileA;

                KnightAttacks[i]  = ((square << 17) | (square >> 15)) & ~Bitboard.FileA;
                KnightAttacks[i] |= ((square << 15) | (square >> 17)) & ~Bitboard.FileH;
                KnightAttacks[i] |= ((square << 10) | (square >> 06)) & ~(Bitboard.FileA | Bitboard.FileB);
                KnightAttacks[i] |= ((square << 06) | (square >> 10)) & ~(Bitboard.FileG | Bitboard.FileH);

                KingAttacks[i]  = (square << 8) | (square >> 8);
                KingAttacks[i] |= ((square << 9) | (square << 1) | (square >> 7)) & ~Bitboard.FileA;
                KingAttacks[i] |= ((square << 7) | (square >> 1) | (square >> 9)) & ~Bitboard.FileH;
            }

            // Sliding Piece
            BishopMagics   = new MagicEntry[64];
            RookMagics     = new MagicEntry[64];
            SlidingAttacks = new ulong[87988];
            
            (ulong Number, uint Offset)[] bishopMagics =
            [
                (0xa7020080601803d8, 60984), (0x13802040400801f1, 66046), (0x0a0080181001f60c, 32910), (0x1840802004238008, 16369),
                (0xc03fe00100000000, 42115), (0x24c00bffff400000,   835), (0x0808101f40007f04, 18910), (0x100808201ec00080, 25911),
                (0xffa2feffbfefb7ff, 63301), (0x083e3ee040080801, 16063), (0xc0800080181001f8, 17481), (0x0440007fe0031000, 59361),
                (0x2010007ffc000000, 18735), (0x1079ffe000ff8000, 61249), (0x3c0708101f400080, 68938), (0x080614080fa00040, 61791),
                (0x7ffe7fff817fcff9, 21893), (0x7ffebfffa01027fd, 62068), (0x53018080c00f4001, 19829), (0x407e0001000ffb8a, 26091),
                (0x201fe000fff80010, 15815), (0xffdfefffde39ffef, 16419), (0xcc8808000fbf8002, 59777), (0x7ff7fbfff8203fff, 16288),
                (0x8800013e8300c030, 33235), (0x0420009701806018, 15459), (0x7ffeff7f7f01f7fd, 15863), (0x8700303010c0c006, 75555),
                (0xc800181810606000, 79445), (0x20002038001c8010, 15917), (0x087ff038000fc001,  8512), (0x00080c0c00083007, 73069),
                (0x00000080fc82c040, 16078), (0x000000407e416020, 19168), (0x00600203f8008020, 11056), (0xd003fefe04404080, 62544),
                (0xa00020c018003088, 80477), (0x7fbffe700bffe800, 75049), (0x107ff00fe4000f90, 32947), (0x7f8fffcff1d007f8, 59172),
                (0x0000004100f88080, 55845), (0x00000020807c4040, 61806), (0x00000041018700c0, 73601), (0x0010000080fc4080, 15546),
                (0x1000003c80180030, 45243), (0xc10000df80280050, 20333), (0xffffffbfeff80fdc, 33402), (0x000000101003f812, 25917),
                (0x0800001f40808200, 32875), (0x084000101f3fd208,  4639), (0x080000000f808081, 17077), (0x0004000008003f80, 62324),
                (0x08000001001fe040, 18159), (0x72dd000040900a00, 61436), (0xfffffeffbfeff81d, 57073), (0xcd8000200febf209, 61025),
                (0x100000101ec10082, 81259), (0x7fbaffffefe0c02f, 64083), (0x7f83fffffff07f7f, 56114), (0xfff1fffffff7ffc1, 57058),
                (0x0878040000ffe01f, 58912), (0x945e388000801012, 22194), (0x0840800080200fda, 70880), (0x100000c05f582008, 11140),
           ];
            (ulong Number, uint Offset)[] rookMagics =
            [
                (0x80280013ff84ffff, 10890), (0x5ffbfefdfef67fff, 50579), (0xffeffaffeffdffff, 62020), (0x003000900300008a, 67322),
                (0x0050028010500023, 80251), (0x0020012120a00020, 58503), (0x0030006000c00030, 51175), (0x0058005806b00002, 83130),
                (0x7fbff7fbfbeafffc, 50430), (0x0000140081050002, 21613), (0x0000180043800048, 72625), (0x7fffe800021fffb8, 80755),
                (0xffffcffe7fcfffaf, 69753), (0x00001800c0180060, 26973), (0x4f8018005fd00018, 84972), (0x0000180030620018, 31958),
                (0x00300018010c0003, 69272), (0x0003000c0085ffff, 48372), (0xfffdfff7fbfefff7, 65477), (0x7fc1ffdffc001fff, 43972),
                (0xfffeffdffdffdfff, 57154), (0x7c108007befff81f, 53521), (0x20408007bfe00810, 30534), (0x0400800558604100, 16548),
                (0x0040200010080008, 46407), (0x0010020008040004, 11841), (0xfffdfefff7fbfff7, 21112), (0xfebf7dfff8fefff9, 44214),
                (0xc00000ffe001ffe0, 57925), (0x4af01f00078007c3, 29574), (0xbffbfafffb683f7f, 17309), (0x0807f67ffa102040, 40143),
                (0x200008e800300030, 64659), (0x0000008780180018, 70469), (0x0000010300180018, 62917), (0x4000008180180018, 60997),
                (0x008080310005fffa, 18554), (0x4000188100060006, 14385), (0xffffff7fffbfbfff,     0), (0x0000802000200040, 38091),
                (0x20000202ec002800, 25122), (0xfffff9ff7cfff3ff, 60083), (0x000000404b801800, 72209), (0x2000002fe03fd000, 67875),
                (0xffffff6ffe7fcffd, 56290), (0xbff7efffbfc00fff, 43807), (0x000000100800a804, 73365), (0x6054000a58005805, 76398),
                (0x0829000101150028, 20024), (0x00000085008a0014,  9513), (0x8000002b00408028, 24324), (0x4000002040790028, 22996),
                (0x7800002010288028, 23213), (0x0000001800e08018, 56002), (0xa3a80003f3a40048, 22809), (0x2003d80000500028, 44545),
                (0xfffff37eefefdfbe, 36072), (0x40000280090013c1,  4750), (0xbf7ffeffbffaf71f,  6014), (0xfffdffff777b7d6e, 36054),
                (0x48300007e8080c02, 78538), (0xafe0000fff780402, 28745), (0xee73fffbffbb77fe,  8555), (0x0002000308482882,  1009),
            ];

            for (byte i = 0; i < 64; i++)
            {
                SetMagic(isBishop: true,  squareIndex: i, bishopMagics[i].Number, bishopMagics[i].Offset);
                SetMagic(isBishop: false, squareIndex: i, rookMagics  [i].Number, rookMagics  [i].Offset);
            }
        }

        public static ulong GetAttacks(byte pieceType, byte squareIndex, ulong occupancy)
        {
            return pieceType switch
            {
                PieceType.Knight => KnightAttacks[squareIndex],
                PieceType.Bishop => GetBishopAttacks(squareIndex, occupancy),
                PieceType.Rook   => GetRookAttacks(squareIndex, occupancy),
                PieceType.Queen  => GetQueenAttacks(squareIndex, occupancy),
                PieceType.King   => KingAttacks[squareIndex],
                _ => 0,
            };
        }

        public static ulong GetBishopAttacks(byte squareIndex, ulong occupancy)
        {
            MagicEntry entry = BishopMagics[squareIndex];
            return SlidingAttacks[entry.Offset + (int)(((occupancy | entry.Mask) * entry.Magic) >> 55)];
        }

        public static ulong GetRookAttacks(byte squareIndex, ulong occupancy)
        {
            MagicEntry entry = RookMagics[squareIndex];
            return SlidingAttacks[entry.Offset + (int)(((occupancy | entry.Mask) * entry.Magic) >> 52)];
        }

        public static ulong GetQueenAttacks(byte squareIndex, ulong occupancy)
        {
            return GetBishopAttacks(squareIndex, occupancy) | GetRookAttacks(squareIndex, occupancy);
        }

        public static ulong CalculateXrayBishopAttacks(byte squareIndex, ulong occupancy, ulong blockers)
        {
            ulong attacks = GetBishopAttacks(squareIndex, occupancy);
            blockers &= attacks;
            return attacks ^ GetBishopAttacks(squareIndex, occupancy ^ blockers);
        }

        public static ulong CalculateXrayRookAttacks(byte squareIndex, ulong occupancy, ulong blockers)
        {
            ulong attacks = GetRookAttacks(squareIndex, occupancy);
            blockers &= attacks;
            return attacks ^ GetRookAttacks(squareIndex, occupancy ^ blockers);
        }

        private static void SetMagic(bool isBishop, int squareIndex, ulong magicNumber, uint offset)
        {
            ulong mask  = isBishop ? CalculateBishopMask(squareIndex) : CalculateRookMask(squareIndex);
            int   shift = isBishop ? 9 : 12;
            int   count = 1 << shift;
            
            MagicEntry entry = new(~mask, magicNumber, offset); // Black Magic Bitboard

            if (isBishop) BishopMagics[squareIndex] = entry;
            else          RookMagics  [squareIndex] = entry;

            ulong[] occupancies = new ulong[count], attacks = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                occupancies[i] = IndexToOccupancy(i, mask);
                attacks    [i] = isBishop ? CalculateBishopAttacks(squareIndex, occupancies[i])
                                          : CalculateRookAttacks  (squareIndex, occupancies[i]);
            }

            for (int i = 0; i < count; i++)
            {
                int index = (int)(((occupancies[i] | entry.Mask) * entry.Magic) >> (64 - shift));
                SlidingAttacks[entry.Offset + index] = attacks[i];
            }

            static ulong IndexToOccupancy(int index, ulong mask)
            {
                ulong occupancy = 0UL;

                for (int i = 0; mask > 0; i++)
                {
                    if ((index & (1 << i)) != 0)
                        occupancy |= 1UL << System.Numerics.BitOperations.TrailingZeroCount(mask);
                    mask &= mask - 1;
                }

                return occupancy;
            }

            static ulong CalculateRookMask(int squareIndex)
            {
                ulong mask = 0;

                int rank = squareIndex >> 3;
                int file = squareIndex & 7;

                for (int r = rank + 1; r <= 6; r++) mask |= 1UL << (file + (8 * r));
                for (int r = rank - 1; r >= 1; r--) mask |= 1UL << (file + (8 * r));
                for (int f = file + 1; f <= 6; f++) mask |= 1UL << (f + (8 * rank));
                for (int f = file - 1; f >= 1; f--) mask |= 1UL << (f + (8 * rank));

                return mask;
            }

            static ulong CalculateBishopMask(int squareIndex)
            {
                ulong mask = 0;

                int rank = squareIndex >> 3;
                int file = squareIndex & 7;

                for (int r = rank + 1, f = file + 1; r <= 6 && f <= 6; r++, f++) mask |= 1UL << (f + (8 * r));
                for (int r = rank + 1, f = file - 1; r <= 6 && f >= 1; r++, f--) mask |= 1UL << (f + (8 * r));
                for (int r = rank - 1, f = file + 1; r >= 1 && f <= 6; r--, f++) mask |= 1UL << (f + (8 * r));
                for (int r = rank - 1, f = file - 1; r >= 1 && f >= 1; r--, f--) mask |= 1UL << (f + (8 * r));

                return mask;
            }

            static ulong CalculateRookAttacks(int squareIndex, ulong occupancy)
            {
                ulong attack, attacks = 0;

                int rank = squareIndex >> 3;
                int file = squareIndex & 7;

                // 右
                for (int r = rank + 1; r < 8; r++)
                {
                    attack = 1UL << (file + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 左
                for (int r = rank - 1; r >= 0; r--)
                {
                    attack = 1UL << (file + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 上
                for (int f = file + 1; f < 8; f++)
                {
                    attack = 1UL << (f + (8 * rank));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 下
                for (int f = file - 1; f >= 0; f--)
                {
                    attack = 1UL << (f + (8 * rank));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                return attacks;
            }

            static ulong CalculateBishopAttacks(int squareIndex, ulong occupancy)
            {
                ulong attack, attacks = 0;

                int rank = squareIndex >> 3;
                int file = squareIndex & 7;

                // 右上
                for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
                {
                    attack = 1UL << (f + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 右下
                for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
                {
                    attack = 1UL << (f + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 左上
                for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
                {
                    attack = 1UL << (f + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                // 左下
                for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
                {
                    attack = 1UL << (f + (8 * r));
                    attacks |= attack;
                    if ((attack & occupancy) != 0) break;
                }

                return attacks;
            }
        }
    }
}