using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal static class Zobrist
    {
        private static class Xorshift
        {
            private static ulong _seed = 2006_09_04;

            internal static ulong GetRandom()
            {
                _seed ^= _seed << 13;
                _seed ^= _seed >> 7;
                _seed ^= _seed << 17;

                return _seed;
            }
        }

        private static readonly ulong   _turnKey           = Xorshift.GetRandom();
        private static readonly ulong[] _whitePositionKeys = GeneratePositionKeys();
        private static readonly ulong[] _blackPositionKeys = GeneratePositionKeys();
        private static readonly ulong[] _castlingKeys      = GenerateCastlingKeys();
        private static readonly ulong[] _enPassantKeys     = GenerateEnPassantKeys();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetTurnKey()
        {
            return _turnKey;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetPositionKey<TColor>(int pieceType, int squareIndex) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whitePositionKeys[GetPositionIndex(pieceType, squareIndex)]
                                  : _blackPositionKeys[GetPositionIndex(pieceType, squareIndex)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetCastlingRightKey(int castlingRights)
        {
            return _castlingKeys[castlingRights];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetEnPassantKey(int squareIndex)
        {
            return _enPassantKeys[squareIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPositionIndex(int pieceType, int squareIndex)
        {
            return (pieceType << 6) + squareIndex;
        }

        private static ulong[] GeneratePositionKeys()
        {
            ulong[] keys = new ulong[PieceType.Length * Square.Length];
            for (int pieceType = PieceType.Pawn; pieceType < PieceType.Length; pieceType++)
            {
                for (int squareIndex = Square.A1; squareIndex < Square.Length; squareIndex++)
                {
                    keys[GetPositionIndex(pieceType, squareIndex)] = Xorshift.GetRandom();
                }
            }

            // To speed up promotion updates, assign a hash value of zero to pawns on the first and eighth ranks.
            for (int file = 0; file < 8; file++)
            {
                keys[GetPositionIndex(PieceType.Pawn, squareIndex: file)] = 0UL;
                keys[GetPositionIndex(PieceType.Pawn, squareIndex: file + Square.A8)] = 0UL;
            }

            return keys;
        }

        private static ulong[] GenerateCastlingKeys()
        {
            Span<ulong> randoms = stackalloc ulong[4];
            for (int i = 0; i < randoms.Length; i++)
            {
                randoms[i] = Xorshift.GetRandom();
            }

            ulong[] keys = new ulong[Castling.Length];
            for (int castlingRights = 0; castlingRights < keys.Length; castlingRights++)
            {
                for (int right = 0; right < randoms.Length; right++)
                {
                    if ((castlingRights & (1 << right)) != 0) { keys[castlingRights] ^= randoms[right]; }
                }
            }
            
            return keys;
        }

        private static ulong[] GenerateEnPassantKeys()
        {
            ulong[] keys = new ulong[Square.Length];
            for (int squareIndex = 0; squareIndex < keys.Length; squareIndex++)
            {
                keys[squareIndex] = squareIndex < 8 ? Xorshift.GetRandom() : keys[Square.GetFile(squareIndex)];
            }

            return keys;
        }
    }
}
