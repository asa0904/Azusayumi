using Azusayumi.Evaluation;

namespace Azusayumi.GameLogic
{
    internal class Board
    {
        public          byte      Side;
        public readonly ulong[][] Pieces;
        public readonly ulong[]   PiecesByColor;
        public          ulong     Occupancy;
        public readonly byte[]    PieceTypes;
        public          GameState State;

        private int ply;
        private readonly GameState[] histories;

        public static Board Initial => new("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        public Board(string fen)
        {
            string[] sections = fen.Split(' ');

            // Piece placement
            Pieces        = [new ulong[6], new ulong[6]];
            PiecesByColor = new ulong[2];
            PieceTypes    = new byte[64];
            for (int i = 0; i < PieceTypes.Length; i++) PieceTypes[i] = PieceType.None;

            int squareIndex = Square.A8;
            foreach (char symbol in sections[0])
            {
                if (char.IsDigit(symbol)) squareIndex += symbol - '0';

                else if (symbol == '/') squareIndex -= 16;

                else
                {
                    (byte color, byte pieceType) = symbol switch
                    {
                        'P' => (Color.White, PieceType.Pawn),
                        'N' => (Color.White, PieceType.Knight),
                        'B' => (Color.White, PieceType.Bishop),
                        'R' => (Color.White, PieceType.Rook),
                        'Q' => (Color.White, PieceType.Queen),
                        'K' => (Color.White, PieceType.King),
                        'p' => (Color.Black, PieceType.Pawn),
                        'n' => (Color.Black, PieceType.Knight),
                        'b' => (Color.Black, PieceType.Bishop),
                        'r' => (Color.Black, PieceType.Rook),
                        'q' => (Color.Black, PieceType.Queen),
                        'k' => (Color.Black, PieceType.King),
                        _ => throw new ArgumentException("Invalid piece symbol in FEN string.")
                    };

                    Pieces[color][pieceType] |= 1UL << squareIndex;
                    PiecesByColor[color]     |= 1UL << squareIndex;
                    Occupancy                |= 1UL << squareIndex;
                    PieceTypes[squareIndex]   = pieceType;

                    squareIndex++;
                }
            }

            // 手番
            Side = sections[1] == "w" ? Color.White : Color.Black;

            // GameState
            byte castlingRights = 0;
            foreach (char right in sections[2])
            {
                switch (right)
                {
                    case 'K': castlingRights |= Castling.WhiteKingCastle;  break;
                    case 'Q': castlingRights |= Castling.WhiteQueenCastle; break;
                    case 'k': castlingRights |= Castling.BlackKingCastle;  break;
                    case 'q': castlingRights |= Castling.BlackQueenCastle; break;
                    default: break;
                }
            }
            byte enPassantIndex = sections[3] == "-" ? Square.None : Square.ToByte(sections[3]);
            byte halfMoveClock  = byte.Parse(sections[4]);

            State = new GameState()
            {
                CastleRights   = castlingRights,
                EnPassantIndex = enPassantIndex,
                HalfMoveClock  = halfMoveClock,
            };

            // Ply
            ply = (2 * (int.Parse(sections[5]) - 1)) + Convert.ToInt32(Side == Color.Black);

            if (halfMoveClock < 0 || ply < halfMoveClock)
                throw new FormatException("Invalid halfmove clock or fullmove number in FEN string.");

            // ゲーム履歴
            histories = new GameState[1024];
            for (int i = 0; i < histories.Length; i++) histories[i] = new GameState();
            histories[ply] = State;

            // その他の情報
            State.Key       = Zobrist.CalculateHash(this);
            State.PawnKey   = Zobrist.CalculatePawnHash(this);
            State.KingIndex = (byte)System.Numerics.BitOperations.TrailingZeroCount(Pieces[Side][PieceType.King]);
            State.MidgameValue = CalculatePcsqValue(Weights.MidgamePcsqTable);
            State.EndgameValue = CalculatePcsqValue(Weights.EndgamePcsqTable);
            State.Phases[Color.White] = GamePhase.Calculate(Color.White, this);
            State.Phases[Color.Black] = GamePhase.Calculate(Color.Black, this);
        }

        public void Print()
        {
            Console.WriteLine();

            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    int   squareIndex = file + (8 * rank);
                    ulong square      = 1UL << squareIndex;
                    int   pieceType   = PieceTypes[squareIndex];
                    
                    char simble = pieceType switch
                    {
                        PieceType.Pawn   => 'P',
                        PieceType.Knight => 'N',
                        PieceType.Bishop => 'B',
                        PieceType.Rook   => 'R',
                        PieceType.Queen  => 'Q',
                        PieceType.King   => 'K',
                        _ => '.'
                    };

                    if (file == 0) Console.Write($"{rank + 1} | ");

                    Console.ForegroundColor = (square & PiecesByColor[Color.White]) != 0 ? ConsoleColor.White
                                            : (square & PiecesByColor[Color.Black]) != 0 ? ConsoleColor.DarkCyan
                                                                                         : ConsoleColor.DarkGray;
                    Console.Write($"{simble} ");
                    Console.ResetColor();

                    if (file == 7) Console.WriteLine();
                }
            }

            Console.WriteLine("   ----------------\n    a b c d e f g h\n");
        }

        public void MakeMove(Move move)
        {
            State = histories[++ply].Initialize(State);

            int   up           = 8 - (Side << 4);
            byte  opponent     = (byte)(Side ^ 1);
            byte  moveType     = move.Type;
            byte  originIndex  = move.OriginIndex;
            byte  targetIndex  = move.TargetIndex;
            byte  pieceType    = PieceTypes[originIndex];
            byte  capturedType = moveType == MoveType.EnPassant ? PieceType.Pawn : PieceTypes[targetIndex];
            ulong originBoard  = 1UL << originIndex;
            ulong targetBoard  = 1UL << targetIndex;

            State.HalfMoveClock++;

            // 駒の位置の更新
            if (moveType == MoveType.Castling) DoCastle(targetIndex);

            Pieces[Side][pieceType] ^= originBoard | targetBoard;
            PiecesByColor[Side]     ^= originBoard | targetBoard;
            Occupancy               ^= originBoard | targetBoard;

            PieceTypes[originIndex] = PieceType.None;
            PieceTypes[targetIndex] = pieceType;

            State.Key ^= Zobrist.PositionKeys[Side][pieceType][originIndex]
                       ^ Zobrist.PositionKeys[Side][pieceType][targetIndex];

            State.MidgameValue += Weights.MidgamePcsqTable[pieceType].Values[Side][targetIndex]
                                - Weights.MidgamePcsqTable[pieceType].Values[Side][originIndex];
            State.EndgameValue += Weights.EndgamePcsqTable[pieceType].Values[Side][targetIndex]
                                - Weights.EndgamePcsqTable[pieceType].Values[Side][originIndex];

            // キャプチャの処理
            if (capturedType != PieceType.None)
            {
                int capturedIndex = targetIndex;

                if (capturedType == PieceType.Pawn)
                {
                    if (moveType == MoveType.EnPassant)
                    {
                        capturedIndex -= up;
                        PieceTypes[capturedIndex] = PieceType.None;
                    }

                    State.PawnKey ^= Zobrist.PositionKeys[opponent][PieceType.Pawn][capturedIndex];
                }

                Pieces[opponent][capturedType] ^= 1UL << capturedIndex;
                PiecesByColor[opponent]        ^= 1UL << capturedIndex;
                Occupancy                      ^= 1UL << capturedIndex;

                State.CapturedPiece = capturedType;
                State.HalfMoveClock = 0;
                State.Key ^= Zobrist.PositionKeys[opponent][capturedType][capturedIndex];

                State.MidgameValue -= Weights.MidgamePcsqTable[capturedType].Values[opponent][capturedIndex];
                State.EndgameValue -= Weights.EndgamePcsqTable[capturedType].Values[opponent][capturedIndex];

                State.Phases[opponent] -= GamePhase.Weights[capturedType];
            }

            // アンパッサン可能マスのリセット
            if (State.EnPassantIndex != Square.None)
            {
                State.Key ^= Zobrist.EnPassantKeys[State.EnPassantIndex];
                State.EnPassantIndex = Square.None;
            }

            if (pieceType == PieceType.Pawn)
            {
                State.PawnKey ^= Zobrist.PositionKeys[Side][PieceType.Pawn][originIndex]
                               ^ Zobrist.PositionKeys[Side][PieceType.Pawn][targetIndex];

                // アンパッサン可能マスのセット
                if ((targetIndex ^ originIndex) == 16
                 && (Attack.PawnAttacks[Side][targetIndex - up] & Pieces[opponent][PieceType.Pawn]) != 0)
                {
                    State.EnPassantIndex = (byte)(targetIndex - up);
                    State.Key ^= Zobrist.EnPassantKeys[State.EnPassantIndex];
                }

                // プロモーションの処理
                if (moveType == MoveType.Promotion)
                {
                    byte promotionType = move.PromotionType;

                    Pieces[Side][PieceType.Pawn] ^= targetBoard;
                    Pieces[Side][promotionType]  ^= targetBoard;
                    PieceTypes[targetIndex] = promotionType;

                    State.Key ^= Zobrist.PositionKeys[Side][promotionType][targetIndex];

                    State.MidgameValue += Weights.MidgamePcsqTable[promotionType] .Values[Side][targetIndex]
                                        - Weights.MidgamePcsqTable[PieceType.Pawn].Values[Side][targetIndex];
                    State.EndgameValue += Weights.EndgamePcsqTable[promotionType] .Values[Side][targetIndex]
                                        - Weights.EndgamePcsqTable[PieceType.Pawn].Values[Side][targetIndex];

                    State.Phases[Side] += GamePhase.Weights[promotionType];
                }

                State.HalfMoveClock = 0;
            }

            // キャスリング権の更新
            byte castleRightsMask = (byte)(Castling.RightMasks[originIndex] & Castling.RightMasks[targetIndex]);
            if (State.CastleRights > 0 && castleRightsMask != 0b1111)
            {
                State.Key ^= Zobrist.CastleRightKeys[State.CastleRights];
                State.CastleRights &= castleRightsMask;
                State.Key ^= Zobrist.CastleRightKeys[State.CastleRights];
            }

            // 手番の更新
            Side = opponent;
            State.Key ^= Zobrist.TurnKey;

            State.KingIndex = (byte)System.Numerics.BitOperations.TrailingZeroCount(Pieces[Side][PieceType.King]);

            // 同形反復の確認
            if (State.HalfMoveClock <= 3) return;

            // 非可逆的な手が指された盤面まで2手おきに遡る
            int repetitionCount = 1;
            for (int i = 4; i <= State.HalfMoveClock; i += 2)
            {
                if (histories[ply - i].Key == State.Key)
                    if (++repetitionCount == 3) { State.IsThreefold = true; break; }
            }
        }

        public void UnmakeMove(Move move)
        {
            byte opponent = Side;

            // 手番の復元
            Side ^= 1;

            int   up          = 8 - (Side << 4);
            byte  moveType    = move.Type;
            byte  originIndex = move.OriginIndex;
            byte  targetIndex = move.TargetIndex;
            byte  pieceType   = PieceTypes[targetIndex];
            ulong originBoard = 1UL << originIndex;
            ulong targetBoard = 1UL << targetIndex;

            // プロモーションしたポーンの復元
            if (moveType == MoveType.Promotion)
            {
                pieceType = PieceType.Pawn;

                Pieces[Side][PieceType.Pawn]     ^= targetBoard;
                Pieces[Side][move.PromotionType] ^= targetBoard;

                PieceTypes[targetIndex] = PieceType.Pawn;
            }

            // 駒の位置の復元
            if (moveType == MoveType.Castling) DoCastle(targetIndex);

            Pieces[Side][pieceType] ^= targetBoard | originBoard;
            PiecesByColor[Side]     ^= targetBoard | originBoard;
            Occupancy               ^= targetBoard | originBoard;

            PieceTypes[originIndex] = pieceType;
            PieceTypes[targetIndex] = PieceType.None;

            // キャプチャされた駒の復元
            if (State.CapturedPiece != PieceType.None)
            {
                if (moveType == MoveType.EnPassant) targetIndex = (byte)(targetIndex - up);

                Pieces[opponent][State.CapturedPiece] ^= 1UL << targetIndex;
                PiecesByColor[opponent]               ^= 1UL << targetIndex;
                Occupancy                             ^= 1UL << targetIndex;

                PieceTypes[targetIndex] = State.CapturedPiece;
            }

            // state の復元
            State = histories[--ply];
        }

        public void MakeNullMove()
        {
            State = histories[++ply].Initialize(State);

            // アンパッサン可能マスのリセット
            if (State.EnPassantIndex != Square.None)
            {
                State.Key ^= Zobrist.EnPassantKeys[State.EnPassantIndex];
                State.EnPassantIndex = Square.None;
            }

            //手番の更新
            Side ^= 1;
            State.Key ^= Zobrist.TurnKey;

            State.KingIndex = (byte)System.Numerics.BitOperations.TrailingZeroCount(Pieces[Side][PieceType.King]);
        }

        public void UnmakeNullMove()
        {
            //手番の復元
            Side ^= 1;

            // state の復元
            State = histories[--ply];
        }

        public bool IsChecked()
        {
            int  opponent  = Side ^ 1;
            byte kingIndex = State.KingIndex;

            return (Attack.GetRookAttacks(kingIndex, Occupancy)
                  & (Pieces[opponent][PieceType.Rook] | Pieces[opponent][PieceType.Queen])) != 0

                || (Attack.GetBishopAttacks(kingIndex, Occupancy)
                  & (Pieces[opponent][PieceType.Bishop] | Pieces[opponent][PieceType.Queen])) != 0

                || (Attack.KnightAttacks[kingIndex] & Pieces[opponent][PieceType.Knight]) != 0

                || (Attack.PawnAttacks[Side][kingIndex] & Pieces[opponent][PieceType.Pawn]) != 0;
        }

        public bool IsKillerLegal(Move quietMove)
        {
            byte  originIndex = quietMove.OriginIndex;
            byte  targetIndex = quietMove.TargetIndex;

            // 移動元に味方の駒がないまたはキャプチャの場合はキラーではない
            if (((1UL << originIndex) & PiecesByColor[Side]) == 0 || PieceTypes[targetIndex] != PieceType.None)
                return false;

            byte  pieceType   = PieceTypes[originIndex];
            ulong originBoard = 1UL << originIndex;
            ulong targetBoard = 1UL << targetIndex;
            byte  kingIndex   = State.KingIndex;

            // 移動先が合法か確認
            if (pieceType == PieceType.Pawn)
            {
                // プロモーションは除く
                if ((targetBoard & (Bitboard.Rank1 | Bitboard.Rank8)) != 0) return false;

                ulong singlePush, doublePush;
                if (Side == Color.White)
                {
                    singlePush = (originBoard << 8) & ~Occupancy;
                    doublePush = (singlePush  << 8) & ~Occupancy & Bitboard.Rank4;
                }
                else
                {
                    singlePush = (originBoard >> 8) & ~Occupancy;
                    doublePush = (singlePush  >> 8) & ~Occupancy & Bitboard.Rank5;
                }

                if ((targetBoard & (singlePush | doublePush)) == 0) return false;
            }
            else
            {
                if ((targetBoard & Attack.GetAttacks(pieceType, originIndex, Occupancy)) == 0)
                    return false;
            }

            // 移動後にキングが攻撃されないか確認
            bool IsAttacked(byte squareIndex)
            {
                int   opponent  = Side ^ 1;
                ulong occupancy = Occupancy ^ (originBoard | targetBoard);

                return (Attack.GetRookAttacks(squareIndex, occupancy)
                      & (Pieces[opponent][PieceType.Rook] | Pieces[opponent][PieceType.Queen])) != 0

                    || (Attack.GetBishopAttacks(squareIndex, occupancy)
                      & (Pieces[opponent][PieceType.Bishop] | Pieces[opponent][PieceType.Queen])) != 0

                    || (Attack.KnightAttacks[squareIndex] & Pieces[opponent][PieceType.Knight]) != 0

                    || (Attack.PawnAttacks[Side][squareIndex] & Pieces[opponent][PieceType.Pawn]) != 0

                    || (Attack.KingAttacks[squareIndex] & Pieces[opponent][PieceType.King]) != 0;
            }

            if (pieceType == PieceType.King) kingIndex = targetIndex;
            
            bool isLegal = true;
            Pieces[Side][pieceType] ^= originBoard | targetBoard;
            if (IsAttacked(kingIndex)) isLegal = false;
            Pieces[Side][pieceType] ^= originBoard | targetBoard;

            return isLegal;
        }

        public ulong CalculateAttackedBoard()
        {
            int   opponent      = Side ^ 1;
            ulong attackedBoard = 0;

            // ポーンの利きの計算
            ulong pawns = Pieces[opponent][PieceType.Pawn];
            if (opponent == Color.White)
            {
                attackedBoard |= pawns << 9 & ~Bitboard.FileA;
                attackedBoard |= pawns << 7 & ~Bitboard.FileH;
            }
            else
            {
                attackedBoard |= pawns >> 9 & ~Bitboard.FileH;
                attackedBoard |= pawns >> 7 & ~Bitboard.FileA;
            }

            // ポーン以外の利きの計算
            ulong pieces;
            ulong occupancy = Occupancy ^ (1UL << State.KingIndex);

            pieces = Pieces[opponent][PieceType.Knight];
            while (pieces > 0) attackedBoard |= Attack.KnightAttacks[Bitboard.PopLSB(ref pieces)];

            pieces = Pieces[opponent][PieceType.Bishop] | Pieces[opponent][PieceType.Queen];
            while (pieces > 0) attackedBoard |= Attack.GetBishopAttacks(Bitboard.PopLSB(ref pieces), occupancy);

            pieces = Pieces[opponent][PieceType.Rook] | Pieces[opponent][PieceType.Queen];
            while (pieces > 0) attackedBoard |= Attack.GetRookAttacks(Bitboard.PopLSB(ref pieces), occupancy);

            pieces = Pieces[opponent][PieceType.King];
            attackedBoard |= Attack.KingAttacks[System.Numerics.BitOperations.TrailingZeroCount(pieces)];

            return attackedBoard;
        }

        public ulong CalculateCheckers()
        {
            int  opponent  = Side ^ 1;
            byte kingIndex = State.KingIndex;

            return (Pieces[opponent][PieceType.Pawn]   & Attack.PawnAttacks[Side][kingIndex])
                 | (Pieces[opponent][PieceType.Knight] & Attack.KnightAttacks[kingIndex])
                 | (Pieces[opponent][PieceType.Bishop] & Attack.GetBishopAttacks(kingIndex, Occupancy))
                 | (Pieces[opponent][PieceType.Rook]   & Attack.GetRookAttacks(kingIndex, Occupancy))
                 | (Pieces[opponent][PieceType.Queen]  & Attack.GetQueenAttacks(kingIndex, Occupancy));
        }

        public ulong CalculatePinnedPieces()
        {
            // ピンしている駒の計算
            int   opponent     = Side ^ 1;
            byte  kingIndex    = State.KingIndex;
            ulong playerPieces = PiecesByColor[Side];
            ulong pinners      = 0;

            pinners |= Attack.CalculateXrayRookAttacks(kingIndex, Occupancy, playerPieces)
                    & (Pieces[opponent][PieceType.Rook] | Pieces[opponent][PieceType.Queen]);

            pinners |= Attack.CalculateXrayBishopAttacks(kingIndex, Occupancy, playerPieces)
                    & (Pieces[opponent][PieceType.Bishop] | Pieces[opponent][PieceType.Queen]);

            // ピンされている駒の計算
            ulong pinnedPieces = 0;
            while (pinners > 0)
            {
                byte  pinnerIndex = Bitboard.PopLSB(ref pinners);
                ulong pinnedLine  = Bitboard.BetweenLines[kingIndex][pinnerIndex] | (1UL << pinnerIndex);

                pinnedPieces |= playerPieces & pinnedLine;
            }

            return pinnedPieces;
        }

        public bool CanEnPassant(int originIndex, int targetIndex, int up)
        {
            int   opponent  = Side ^ 1;
            ulong occupancy = (Occupancy ^ (1UL << originIndex) ^ (1UL << targetIndex - up)) | (1UL << targetIndex);

            return (Attack.GetRookAttacks(State.KingIndex, occupancy)
                 & (Pieces[opponent][PieceType.Rook] | Pieces[opponent][PieceType.Queen])) == 0;
        }

        public bool CanCastle(int castleType, ulong attackedBoard)
        {
            // キングまたはルークが初期位置から動いている
            if ((State.CastleRights & castleType) == 0) return false;

            // キングとルークの間に駒がある
            if ((Castling.Path[castleType] & Occupancy) != 0)
                return false;

            // キングまたはキングの通過位置が攻撃されている
            return (Castling.PassedSquares[castleType] & attackedBoard) == 0;
        }

        private void DoCastle(byte kingTargetIndex)
        {
            byte rookOriginIndex = 0, rookTargetIndex = 0;

            switch (kingTargetIndex)
            {
                case Square.G1:
                    rookOriginIndex = Square.H1;
                    rookTargetIndex = Square.F1;
                    break;
                
                case Square.C1:
                    rookOriginIndex = Square.A1;
                    rookTargetIndex = Square.D1;
                    break;
                
                case Square.G8:
                    rookOriginIndex = Square.H8;
                    rookTargetIndex = Square.F8;
                    break;
                
                case Square.C8:
                    rookOriginIndex = Square.A8;
                    rookTargetIndex = Square.D8;
                    break;
                
                default:
                    break;
            }

            Pieces[Side][PieceType.Rook] ^= (1UL << rookOriginIndex) | (1UL << rookTargetIndex);
            PiecesByColor[Side]          ^= (1UL << rookOriginIndex) | (1UL << rookTargetIndex);
            Occupancy                    ^= (1UL << rookOriginIndex) | (1UL << rookTargetIndex);

            (PieceTypes[rookOriginIndex], PieceTypes[rookTargetIndex]) = (PieceTypes[rookTargetIndex], PieceTypes[rookOriginIndex]);

            State.Key ^= Zobrist.PositionKeys[Side][PieceType.Rook][rookOriginIndex]
                       ^ Zobrist.PositionKeys[Side][PieceType.Rook][rookTargetIndex];

            State.MidgameValue += Weights.MidgamePcsqTable[PieceType.Rook].Values[Side][rookTargetIndex]
                                - Weights.MidgamePcsqTable[PieceType.Rook].Values[Side][rookOriginIndex];
            State.EndgameValue += Weights.EndgamePcsqTable[PieceType.Rook].Values[Side][rookTargetIndex]
                                - Weights.EndgamePcsqTable[PieceType.Rook].Values[Side][rookOriginIndex];
        }

        private int CalculatePcsqValue(SquareTable[] pcsqTable)
        {
            int value = 0;

            for (byte color = Color.White; color <= Color.Black; color++)
            {
                ulong pieces = PiecesByColor[color];
                while (pieces > 0)
                {
                    byte squareIndex = Bitboard.PopLSB(ref pieces);
                    byte pieceType   = PieceTypes[squareIndex];
                    
                    value += pcsqTable[pieceType].Values[color][squareIndex];
                }
            }

            return value;
        }
    }
}
