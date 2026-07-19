using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal static class Attacks
    {
        private static readonly ulong[] _whitePawnAttacks = GenerateWhitePawnTable();
        private static readonly ulong[] _blackPawnAttacks = GenerateBlackPawnTable();
        private static readonly ulong[] _knightAttacks    = GenerateKnightTable();
        private static readonly ulong[] _kingAttacks      = GenerateKingTable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetPawnAttacks<TColor>(int squareIndex) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whitePawnAttacks[squareIndex] : _blackPawnAttacks[squareIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetKnightAttacks(int squareIndex)
        {
            return _knightAttacks[squareIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetKingAttacks(int squareIndex)
        {
            return _kingAttacks[squareIndex];
        }

        private static ulong[] GenerateWhitePawnTable()
        {
            ulong[] attacks = new ulong[Square.Length];
            for (int i = 0; i < attacks.Length; i++)
            {
                ulong square = 1UL << i;
                attacks[i] = (square << 9) & ~Bitboard.FileA | (square << 7) & ~Bitboard.FileH;
            }

            return attacks;
        }

        private static ulong[] GenerateBlackPawnTable()
        {
            ulong[] attacks = new ulong[Square.Length];
            for (int i = 0; i < attacks.Length; i++)
            {
                ulong square = 1UL << i;
                attacks[i] = (square >> 9) & ~Bitboard.FileH | (square >> 7) & ~Bitboard.FileA;
            }

            return attacks;
        }

        private static ulong[] GenerateKnightTable()
        {
            ulong[] attacks = new ulong[Square.Length];
            for (int i = 0; i < attacks.Length; i++)
            {
                ulong square = 1UL << i;
                attacks[i] = (((square << 17) | (square >> 15)) & ~Bitboard.FileA)
                           | (((square << 15) | (square >> 17)) & ~Bitboard.FileH)
                           | (((square << 10) | (square >>  6)) & ~(Bitboard.FileA | Bitboard.FileB))
                           | (((square <<  6) | (square >> 10)) & ~(Bitboard.FileG | Bitboard.FileH));
            }

            return attacks;
        }

        private static ulong[] GenerateKingTable()
        {
            ulong[] attacks = new ulong[Square.Length];
            for (int i = 0; i < attacks.Length; i++)
            {
                ulong square = 1UL << i;
                attacks[i] = (square << 8) | (square >> 8)
                           | (((square << 9) | (square << 1) | (square >> 7)) & ~Bitboard.FileA)
                           | (((square << 7) | (square >> 1) | (square >> 9)) & ~Bitboard.FileH);
            }

            return attacks;
        }
    }
}
