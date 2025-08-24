namespace Azusayumi.GameLogic
{
    internal struct Move
    {
        public static readonly Move Null = new(0, 0);

        private ushort data;

        public readonly byte OriginIndex => (byte)((data >> 6) & 0x003F);

        public readonly byte TargetIndex => (byte)(data & 0x003F);

        public readonly byte PromotionType => (byte)((data >> 12 & 0x0003) + PieceType.Knight);

        public readonly byte Type => (byte)((data >> 14) & 0x003);

        public Move(byte originIndex, byte targetIndex)
        {
            data &= 0xF03F;
            data |= (ushort)(originIndex << 6);

            data &= 0xFFC0;
            data |= targetIndex;
        }

        public Move MakePromotion(byte promotionType)
        {
            data &= 0xCFFF;
            data |= (ushort)((promotionType - PieceType.Knight) << 12);

            data &= 0x3FFF;
            data |= MoveType.Promotion << 14;

            return this;
        }

        public Move MakeCastling()
        {
            data &= 0x3FFF;
            data |= MoveType.Castling << 14;

            return this;
        }

        public Move MakeEnPassant()
        {
            data &= 0x3FFF;
            data |= MoveType.EnPassant << 14;

            return this;
        }

        public static bool operator ==(Move left, Move right)
        {
            return left.data == right.data;
        }

        public static bool operator !=(Move left, Move right)
        {
            return left.data != right.data;
        }

        public override readonly string ToString()
        {
            if (data == 0) return "0000";

            string move = Square.ToString(OriginIndex) + Square.ToString(TargetIndex);

            if (Type == MoveType.Promotion)
            {
                string promotionType = PromotionType switch
                {
                    PieceType.Knight => "n",
                    PieceType.Bishop => "b",
                    PieceType.Rook   => "r",
                    PieceType.Queen  => "q",
                    _ => ""
                };

                move += promotionType;
            }

            return move;
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is Move other) return this == other;

            return false;
        }

        public override readonly int GetHashCode()
        {
            return data.GetHashCode();
        }
    }
}