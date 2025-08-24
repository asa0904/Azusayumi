namespace Azusayumi.GameLogic
{
    internal static class Zobrist
    {
        public static readonly ulong       TurnKey;
        public static readonly ulong       NoPawnKey;
        public static readonly ulong[][][] PositionKeys;
        public static readonly ulong[]     CastleRightKeys;
        public static readonly ulong[]     EnPassantKeys;
        
        static Zobrist()
        {
            TurnKey   = Xorshift.GetRandom();
            NoPawnKey = Xorshift.GetRandom();

            // 駒の位置のハッシュ値の初期化
            PositionKeys = new ulong[2][][];
            for (int color = Color.White; color <= Color.Black; color++)
            {
                PositionKeys[color] = new ulong[6][];
                for (int pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
                {
                    PositionKeys[color][pieceType] = new ulong[64];
                    for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                        PositionKeys[color][pieceType][squareIndex] = Xorshift.GetRandom();
                }
            }

            // 1/8 ランクのポーンのハッシュ値は 0 にする
            for (int color = Color.White; color <= Color.Black; color++)
            {
                for (int file = 0; file < 8; file++)
                {
                    PositionKeys[color][PieceType.Pawn][(8 * 0) + file] = 0;
                    PositionKeys[color][PieceType.Pawn][(8 * 7) + file] = 0;
                }
            }

            // キャスリング権のハッシュ値の初期化
            CastleRightKeys = new ulong[16];
            for (int castleRight = 0; castleRight < CastleRightKeys.Length; castleRight++)
                CastleRightKeys[castleRight] = Xorshift.GetRandom();

            // アンパッサン可能ファイルのハッシュ値の初期化
            EnPassantKeys = new ulong[64];
            for (int file = 0; file < 8; file++) EnPassantKeys[file] = Xorshift.GetRandom();
            for (int squareIndex = 0; squareIndex < EnPassantKeys.Length; squareIndex++)
                EnPassantKeys[squareIndex] = EnPassantKeys[squareIndex & 7];
        }

        public static ulong CalculateHash(Board board)
        {
            ulong hash = 0;

            // 手番
            if (board.Side == Color.Black) hash ^= TurnKey;

            // 駒の位置
            ulong whitePieces = board.PiecesByColor[Color.White];
            while (whitePieces > 0)
            {
                int squareIndex = Bitboard.PopLSB(ref whitePieces);
                int pieceType   = board.PieceTypes[squareIndex];

                hash ^= PositionKeys[Color.White][pieceType][squareIndex];
            }

            ulong blackPieces = board.PiecesByColor[Color.Black];
            while (blackPieces > 0)
            {
                int squareIndex = Bitboard.PopLSB(ref blackPieces);
                int pieceType   = board.PieceTypes[squareIndex];

                hash ^= PositionKeys[Color.Black][pieceType][squareIndex];
            }

            // キャスリング権
            hash ^= CastleRightKeys[board.State.CastleRights];

            // アンパッサン可能ファイル
            if (board.State.EnPassantIndex != Square.None) hash ^= EnPassantKeys[board.State.EnPassantIndex];

            return hash;
        }

        public static ulong CalculatePawnHash(Board board)
        {
            ulong hash = NoPawnKey;
            for (int color = Color.White; color <= Color.Black; color++)
            {
                ulong pawns = board.Pieces[color][PieceType.Pawn];
                while (pawns > 0)
                {
                    int squareIndex = Bitboard.PopLSB(ref pawns);
                    hash ^= PositionKeys[color][PieceType.Pawn][squareIndex];
                }
            }

            return hash;
        }
    }
}