using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal static class MoveType
    {
        internal const int Normal    = 0b00 << 12;
        internal const int Castling  = 0b01 << 12;
        internal const int EnPassant = 0b10 << 12;
        internal const int Promotion = 0b11 << 12;

        internal const int O_O   = 0b1001 << 12;
        internal const int O_O_O = 0b0001 << 12;

        internal const int KnightPromotion = 0b0011 << 12;
        internal const int BishopPromotion = 0b0111 << 12;
        internal const int RookPromotion   = 0b1011 << 12;
        internal const int QueenPromotion  = 0b1111 << 12;
    }

    internal readonly struct Move
    {
        /* Encoding of the upper 4 bits.
         * +-----------+-----------+-------------+
         * |   Extra   | Move type |   Meaning   |
         * +-----------+-----------+-------------+
         * |  0  |  0  |  0  |  0  | Normal      |
         * |  1  |  0  |  0  |  1  | O-O         |
         * |  0  |  0  |  0  |  1  | O-O-O       |
         * |  0  |  0  |  1  |  0  | En passant  |
         * |  0  |  0  |  1  |  1  | N promotion |
         * |  0  |  1  |  1  |  1  | B promotion |
         * |  1  |  0  |  1  |  1  | R promotion |
         * |  1  |  1  |  1  |  1  | Q promotion |
         * +-----------+-----------+-------------+
         */

        public static readonly Move Null = new(0, 0);

        private readonly ushort _packed;

        internal Move(int originIndex, int targetIndex)
        {
            _packed = (ushort)((originIndex << 6) | targetIndex);
        }

        internal Move(int moveType, int originIndex, int targetIndex)
        {
            _packed = (ushort)(moveType | (originIndex << 6) | targetIndex);
        }

        internal readonly int OriginIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_packed >> 6) & 0x003F;
        }

        internal readonly int TargetIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _packed & 0x003F;
        }

        internal readonly int Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _packed & 0x3000;
        }

        internal readonly int PromotionType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_packed >> 14) + PieceType.Knight;
        }

        internal readonly int CastlingOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _packed >> 14;
        }

        public override readonly string ToString()
        {
            string move = $"{Square.ToCoordinate(OriginIndex)}{Square.ToCoordinate(TargetIndex)}";
            if (Type == MoveType.Promotion)
            {
                char promotionType = "pnbrq"[PromotionType];
                return $"{move}{promotionType}";
            }

            return move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Move left, Move right)
        {
            return left._packed == right._packed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Move left, Move right)
        {
            return left._packed != right._packed;
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is Move other) { return this == other; }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return _packed.GetHashCode();
        }
    }
}
