using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    public interface IColor
    {
        static abstract bool IsWhite { get; }

        internal static abstract int Up { get; }

        internal static abstract int UpRight { get; }

        internal static abstract int UpLeft { get; }

        internal static abstract ulong Rank4 { get; }

        internal static abstract ulong Rank7 { get; }

        internal static abstract ulong GetSinglePawnPush(ulong pawns);

        internal static abstract ulong GetPawnRightAttacks(ulong pawns);

        internal static abstract ulong GetPawnLeftAttacks(ulong pawns);
    }

    public readonly struct White : IColor
    {
        public static bool IsWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        public static int Up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 8;
        }

        public static int UpRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 9;
        }

        public static int UpLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 7;
        }

        public static ulong Rank4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Bitboard.Rank4;
        }

        public static ulong Rank7
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Bitboard.Rank7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetSinglePawnPush(ulong pawns)
        {
            return pawns << 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetPawnRightAttacks(ulong pawns)
        {
            return (pawns << 9) & ~Bitboard.FileA;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetPawnLeftAttacks(ulong pawns)
        {
            return (pawns << 7) & ~Bitboard.FileH;
        }
    }

    public readonly struct Black : IColor
    {
        public static bool IsWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        public static int Up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => -8;
        }

        public static int UpRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => -9;
        }

        public static int UpLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => -7;
        }

        public static ulong Rank4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Bitboard.Rank5;
        }

        public static ulong Rank7
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Bitboard.Rank2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetSinglePawnPush(ulong pawns)
        {
            return pawns >> 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetPawnRightAttacks(ulong pawns)
        {
            return (pawns >> 9) & ~Bitboard.FileH;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetPawnLeftAttacks(ulong pawns)
        {
            return (pawns >> 7) & ~Bitboard.FileA;
        }
    }
}
