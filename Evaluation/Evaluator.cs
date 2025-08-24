using Azusayumi.GameLogic;
using System.Numerics;

namespace Azusayumi.Evaluation
{
    internal static class Evaluator
    {
        private static readonly PawnHashTable PawnHashTable = new(sizeKB: 512);

        public static int Evaluate(Board board)
        {
            /**********************
             *   Initialization   *
             **********************/

            // 位置情報の初期化
            ulong wPieces    = board.PiecesByColor[Color.White];
            ulong bPieces    = board.PiecesByColor[Color.Black];
            ulong occupancy  = board.Occupancy;
            ulong wPawns     = board.Pieces[Color.White][PieceType.Pawn];
            ulong bPawns     = board.Pieces[Color.Black][PieceType.Pawn];
            ulong wKnights   = board.Pieces[Color.White][PieceType.Knight];
            ulong bKnights   = board.Pieces[Color.Black][PieceType.Knight];
            ulong wBishops   = board.Pieces[Color.White][PieceType.Bishop];
            ulong bBishops   = board.Pieces[Color.Black][PieceType.Bishop];
            ulong wRooks     = board.Pieces[Color.White][PieceType.Rook];
            ulong bRooks     = board.Pieces[Color.Black][PieceType.Rook];
            ulong wQueens    = board.Pieces[Color.White][PieceType.Queen];
            ulong bQueens    = board.Pieces[Color.Black][PieceType.Queen];
            ulong wKing      = board.Pieces[Color.White][PieceType.King];
            ulong bKing      = board.Pieces[Color.Black][PieceType.King];
            int   wKingIndex = BitOperations.TrailingZeroCount(wKing);
            int   bKingIndex = BitOperations.TrailingZeroCount(bKing);
            ulong wKingArea  = Bitboard.SquaresNearKing[Color.White][wKingIndex];
            ulong bKingArea  = Bitboard.SquaresNearKing[Color.Black][bKingIndex];
            int   wPawnCount = BitOperations.PopCount(wPawns);
            int   bPawnCount = BitOperations.PopCount(bPawns);

            // 評価項の初期化
            int wAttackCount  = 0, bAttackCount  = 0;
            int wAttackWeight = 0, bAttackWeight = 0;

            // Piece square table
            int midValue = board.State.MidgameValue;
            int endValue = board.State.EndgameValue;

            // Tempo bounus
            int result = board.Side == Color.White ? Weights.Tempo : -Weights.Tempo;

            /***********************
             *   Pawn Evaluation   *
             ***********************/
            int pawnScore;
            if ((pawnScore = PawnHashTable.Read(board)) != PawnHashTable.Null) result += pawnScore;
            else
            {
                pawnScore = 0;

                ulong wFrontspans = wPawns << 8;
                wFrontspans |= wFrontspans << 8;
                wFrontspans |= wFrontspans << 16;
                wFrontspans |= wFrontspans << 32;

                ulong bFrontspans = bPawns >> 8;
                bFrontspans |= bFrontspans >> 8;
                bFrontspans |= bFrontspans >> 16;
                bFrontspans |= bFrontspans >> 32;

                // Double pawn
                pawnScore -= Weights.DoublePawn * BitOperations.PopCount(wPawns & wFrontspans);
                pawnScore += Weights.DoublePawn * BitOperations.PopCount(bPawns & bFrontspans);

                bFrontspans |= bFrontspans << 1 & ~Bitboard.FileA;
                bFrontspans |= bFrontspans >> 1 & ~Bitboard.FileH;
                wFrontspans |= wFrontspans << 1 & ~Bitboard.FileA;
                wFrontspans |= wFrontspans >> 1 & ~Bitboard.FileH;

                ulong pawns;

                pawns = wPawns;
                while (pawns > 0)
                {
                    byte squareIndex = Bitboard.PopLSB(ref pawns);

                    ulong rearspans = Attack.PawnAttacks[Color.Black][squareIndex] << 8;
                    rearspans |= rearspans >> 8;
                    rearspans |= rearspans >> 16;
                    rearspans |= rearspans >> 32;

                    // Weak pawn
                    if ((wPawns & rearspans) == 0)
                    {
                        pawnScore += Weights.WeakPawnTable.Values[Color.White][squareIndex];

                        // セミオープンの場合は追加のペナルティ
                        if ((bPawns & wFrontspans & (Bitboard.FileA << (squareIndex & 7))) == 0)
                            pawnScore -= 4;
                    }

                    // Passed pawn
                    if (((1UL << squareIndex) & ~bFrontspans) != 0)
                    {
                        ulong supports = Attack.PawnAttacks[Color.Black][squareIndex];

                        // パスポーンの横か斜め後ろに味方のポーンがある場合はボーナスが1.25倍
                        if ((wPawns & (supports | (supports << 8))) != 0)
                            pawnScore += (Weights.PassedPawnTable.Values[Color.White][squareIndex] * 5) >> 2;
                        else
                            pawnScore += Weights.PassedPawnTable.Values[Color.White][squareIndex];
                    }
                }

                pawns = bPawns;
                while (pawns > 0)
                {
                    byte squareIndex = Bitboard.PopLSB(ref pawns);

                    ulong rearspans = Attack.PawnAttacks[Color.White][squareIndex] >> 8;
                    rearspans |= rearspans << 8;
                    rearspans |= rearspans << 16;
                    rearspans |= rearspans << 32;

                    if ((bPawns & rearspans) == 0)
                    {
                        pawnScore += Weights.WeakPawnTable.Values[Color.Black][squareIndex];
                        if ((wPawns & bFrontspans & (Bitboard.FileA << (squareIndex & 7))) == 0)
                            pawnScore += 4;
                    }

                    if (((1UL << squareIndex) & ~wFrontspans) != 0)
                    {
                        ulong supports = Attack.PawnAttacks[Color.White][squareIndex];

                        if ((bPawns & (supports | (supports >> 8))) != 0)
                            pawnScore += (Weights.PassedPawnTable.Values[Color.Black][squareIndex] * 5) >> 2;
                        else
                            pawnScore += Weights.PassedPawnTable.Values[Color.Black][squareIndex];
                    }
                }

                PawnHashTable.Write(board, pawnScore);

                result += pawnScore;
            }

            /*************************
             *   Knight Evaluation   *
             *************************/
            ulong knights; int knightCount;

            knights = wKnights; knightCount = 0;
            while (knights > 0)
            {
                knightCount++;

                byte squareIndex = Bitboard.PopLSB(ref knights);

                ulong attacks    = Attack.KnightAttacks[squareIndex] & ~wPieces;
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & bKingArea);

                midValue += Weights.MidgameMobilityTable[PieceType.Knight][mobility];
                endValue += Weights.EndgameMobilityTable[PieceType.Knight][mobility];

                if (kingAttack > 0)
                {
                    wAttackCount++;
                    wAttackWeight += Weights.Attackers[PieceType.Knight] * kingAttack;
                }

                switch (squareIndex)
                {
                    // トラップされた場合にペナルティ
                    case Square.A8 when (bPawns & (Bitboard.A7 | Bitboard.C7)) != 0:
                        result -= Weights.KnightTrappedA8; break;

                    case Square.H8 when (bPawns & (Bitboard.H7 | Bitboard.F7)) != 0:
                        result -= Weights.KnightTrappedA8; break;

                    case Square.A7 when (bPawns & Bitboard.A6) != 0 && (bPawns & Bitboard.B7) != 0:
                        result -= Weights.KnightTrappedA7; break;

                    case Square.H7 when (bPawns & Bitboard.H6) != 0 && (bPawns & Bitboard.G7) != 0:
                        result -= Weights.KnightTrappedA7; break;

                    // d4-c4構造を推奨する
                    case Square.C3 when (wPawns & Bitboard.C2) != 0 && (wPawns & Bitboard.D4) != 0 && (wPawns & Bitboard.E4) == 0:
                        result -= Weights.C3Knight; break;
                }
            }
            
            // ナイトペアにペナルティ
            if (knightCount > 1) result -= Weights.KnightPair;

            // ポーン補正
            result += knightCount * Weights.KnightAdjustements[wPawnCount];

            knights = bKnights; knightCount = 0;
            while (knights > 0)
            {
                knightCount++;

                byte squareIndex = Bitboard.PopLSB(ref knights);

                ulong attacks    = Attack.KnightAttacks[squareIndex] & ~bPieces;
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & wKingArea);

                midValue -= Weights.MidgameMobilityTable[PieceType.Knight][mobility];
                endValue -= Weights.EndgameMobilityTable[PieceType.Knight][mobility];

                if (kingAttack > 0)
                {
                    bAttackCount++;
                    bAttackWeight += Weights.Attackers[PieceType.Knight] * kingAttack;
                }

                switch (squareIndex)
                {
                    case Square.A1 when (wPawns & (Bitboard.A2 | Bitboard.C2)) != 0:
                        result += Weights.KnightTrappedA8; break;

                    case Square.H1 when (wPawns & (Bitboard.H2 | Bitboard.F2)) != 0:
                        result += Weights.KnightTrappedA8; break;

                    case Square.A2 when (wPawns & Bitboard.A3) != 0 && (wPawns & Bitboard.B2) != 0:
                        result += Weights.KnightTrappedA7; break;

                    case Square.H2 when (wPawns & Bitboard.H3) != 0 && (wPawns & Bitboard.G2) != 0:
                        result += Weights.KnightTrappedA7; break;

                    case Square.C6 when (bPawns & Bitboard.C7) != 0 && (bPawns & Bitboard.D5) != 0 && (bPawns & Bitboard.E5) == 0:
                        result += Weights.C3Knight; break;
                }
            }

            if (knightCount > 1) result += Weights.KnightPair;

            result -= knightCount * Weights.KnightAdjustements[bPawnCount];

            /*************************
             *   Bishop Evaluation   *
             *************************/
            ulong bishops; int bishopCount;

            bishops = wBishops; bishopCount = 0;
            while (bishops > 0)
            {
                bishopCount++;

                byte squareIndex = Bitboard.PopLSB(ref bishops);

                ulong attacks    = Attack.GetBishopAttacks(squareIndex, occupancy) & ~wPieces;
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & bKingArea);

                midValue += Weights.MidgameMobilityTable[PieceType.Bishop][mobility];
                endValue += Weights.EndgameMobilityTable[PieceType.Bishop][mobility];

                if (kingAttack > 0)
                {
                    wAttackCount++;
                    wAttackWeight += Weights.Attackers[PieceType.Bishop] * kingAttack;
                }

                switch (squareIndex)
                {
                    // トラップされた場合にペナルティ
                    case Square.A7 when (bPawns & Bitboard.B6) != 0:
                        result -= Weights.BishopTrappedA7; break;

                    case Square.H7 when (bPawns & Bitboard.G6) != 0:
                        result -= Weights.BishopTrappedA7; break;

                    case Square.B8 when (bPawns & Bitboard.C7) != 0:
                        result -= Weights.BishopTrappedA7; break;

                    case Square.G8 when (bPawns & Bitboard.F7) != 0:
                        result -= Weights.BishopTrappedA7; break;

                    case Square.A6 when (bPawns & Bitboard.B5) != 0:
                        result -= Weights.BishopTrappedA6; break;

                    case Square.H6 when (bPawns & Bitboard.G5) != 0:
                        result -= Weights.BishopTrappedA6; break;

                    case Square.F1:

                        // キャスリング後のキングの隣のビショップにボーナス
                        if (wKingIndex == Square.G1)
                            result += Weights.ReturningBishop;

                        // センターポーンにブロックされている場合にペナルティ
                        if ((wPawns & Bitboard.E2) != 0 && (occupancy & Bitboard.E3) != 0)
                            result -= Weights.BlockCentralPawn;
                        break;

                    case Square.C1:
                        if (wKingIndex == Square.B1)
                            result += Weights.ReturningBishop;
                        if ((wPawns & Bitboard.D2) != 0 && (occupancy & Bitboard.D3) != 0)
                            result -= Weights.BlockCentralPawn;
                        break;
                }
            }

            // ビショップペアにボーナス
            if (bishopCount > 1) result += Weights.BishopPair;

            bishops = bBishops; bishopCount = 0;
            while (bishops > 0)
            {
                bishopCount++;

                byte squareIndex = Bitboard.PopLSB(ref bishops);

                ulong attacks    = Attack.GetBishopAttacks(squareIndex, occupancy) & ~bPieces;
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & wKingArea);

                midValue -= Weights.MidgameMobilityTable[PieceType.Bishop][mobility];
                endValue -= Weights.EndgameMobilityTable[PieceType.Bishop][mobility];

                if (kingAttack > 0)
                {
                    bAttackCount++;
                    bAttackWeight += Weights.Attackers[PieceType.Bishop] * kingAttack;
                }

                switch (squareIndex)
                {
                    case Square.A2 when (wPawns & Bitboard.B3) != 0:
                        result += Weights.BishopTrappedA7; break;

                    case Square.H2 when (wPawns & Bitboard.G3) != 0:
                        result += Weights.BishopTrappedA7; break;

                    case Square.B1 when (wPawns & Bitboard.C2) != 0:
                        result += Weights.BishopTrappedA7; break;

                    case Square.G1 when (wPawns & Bitboard.F2) != 0:
                        result += Weights.BishopTrappedA7; break;

                    case Square.A3 when (wPawns & Bitboard.B4) != 0:
                        result += Weights.BishopTrappedA6; break;

                    case Square.H3 when (wPawns & Bitboard.G4) != 0:
                        result += Weights.BishopTrappedA6; break;

                    case Square.F8:
                        if ((bPawns & Bitboard.E7) != 0 && (occupancy & Bitboard.E6) != 0)
                            result += Weights.BlockCentralPawn;
                        if (bKingIndex == Square.G8)
                            result -= Weights.ReturningBishop;
                        break;

                    case Square.C8:
                        if ((bPawns & Bitboard.D7) != 0 && (occupancy & Bitboard.D6) != 0)
                            result += Weights.BlockCentralPawn;
                        if (bKingIndex == Square.B8)
                            result -= Weights.ReturningBishop;
                        break;
                }
            }

            if (bishopCount > 1) result -= Weights.BishopPair;

            /***********************
             *   Rook Evaluation   *
             ***********************/
            ulong rooks; int rookCount;

            rooks = wRooks; rookCount = 0;
            while (rooks > 0)
            {
                rookCount++;

                byte squareIndex = Bitboard.PopLSB(ref rooks);

                ulong attacks    = Attack.GetRookAttacks(squareIndex, occupancy) & ~wPieces; 
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & bKingArea);

                midValue += Weights.MidgameMobilityTable[PieceType.Rook][mobility];
                endValue += Weights.EndgameMobilityTable[PieceType.Rook][mobility];

                if (kingAttack > 0)
                {
                    wAttackCount++;
                    wAttackWeight += Weights.Attackers[PieceType.Rook] * kingAttack;
                }

                // オープンまたはセミオープンファイルのルークにボーナス
                ulong file = Bitboard.FileA << (squareIndex & 7);
                if ((wPawns & file) == 0)
                {
                    if ((bPawns & file) == 0) { midValue += Weights.RookOpen; endValue += Weights.RookOpen; }
                    else                      { midValue += Weights.RookHalf; endValue += Weights.RookHalf; }
                }
            }

            // ルークペアにペナルティ
            if (rookCount > 1) result -= Weights.RookPair;

            // ポーン補正
            result += rookCount * Weights.RookAdjustements[wPawnCount];

            rooks = bRooks; rookCount = 0;
            while (rooks > 0)
            {
                rookCount++;

                byte squareIndex = Bitboard.PopLSB(ref rooks);

                ulong attacks    = Attack.GetRookAttacks(squareIndex, occupancy) & ~bPieces; 
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & wKingArea);

                midValue -= Weights.MidgameMobilityTable[PieceType.Rook][mobility];
                endValue -= Weights.EndgameMobilityTable[PieceType.Rook][mobility];

                if (kingAttack > 0)
                {
                    bAttackCount++;
                    bAttackWeight += Weights.Attackers[PieceType.Rook] * kingAttack;
                }

                ulong file = Bitboard.FileA << (squareIndex & 7);
                if ((bPawns & file) == 0)
                {
                    if ((wPawns & file) == 0) { midValue -= Weights.RookOpen; endValue -= Weights.RookOpen; }
                    else                      { midValue -= Weights.RookHalf; endValue -= Weights.RookHalf; }
                }
            }

            if (rookCount > 1) result += Weights.RookPair;

            result -= rookCount * Weights.RookAdjustements[bPawnCount];

            /************************
             *   Queen Evaluation   *
             ************************/
            ulong qweens;

            qweens = wQueens;
            while (qweens > 0)
            {
                byte squareIndex = Bitboard.PopLSB(ref qweens);

                ulong attacks    = Attack.GetQueenAttacks(squareIndex, occupancy) & ~wPieces;
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & bKingArea);

                midValue += Weights.MidgameMobilityTable[PieceType.Queen][mobility];
                endValue += Weights.EndgameMobilityTable[PieceType.Queen][mobility];

                if (kingAttack > 0)
                {
                    wAttackCount++;
                    wAttackWeight += Weights.Attackers[PieceType.Queen] * kingAttack;
                }

                // 早期展開されたクイーンにペナルティ
                if ((squareIndex >> 3) > 2)
                {
                    ulong initialKnights = wKnights & (Bitboard.B1 | Bitboard.G1);
                    ulong initialBishops = wBishops & (Bitboard.C1 | Bitboard.F1);

                    result -= 2 * BitOperations.PopCount(initialKnights | initialBishops);
                }
            }

            qweens = bQueens;
            while (qweens > 0)
            {
                byte squareIndex = Bitboard.PopLSB(ref qweens);

                ulong attacks    = Attack.GetQueenAttacks(squareIndex, occupancy) & ~bPieces; 
                int   mobility   = BitOperations.PopCount(attacks);
                int   kingAttack = BitOperations.PopCount(attacks & wKingArea);

                midValue -= Weights.MidgameMobilityTable[PieceType.Queen][mobility];
                endValue -= Weights.EndgameMobilityTable[PieceType.Queen][mobility];

                if (kingAttack > 0)
                {
                    bAttackCount++;
                    bAttackWeight += Weights.Attackers[PieceType.Queen] * kingAttack;
                }

                if ((squareIndex >> 3) < 7)
                {
                    ulong initialKnights = bKnights & (Bitboard.B8 | Bitboard.G8);
                    ulong initialBishops = bBishops & (Bitboard.C8 | Bitboard.F8);

                    result += 2 * BitOperations.PopCount(initialKnights | initialBishops);
                }
            }

            /***********************
             *   King Evaluation   *
             ***********************/

            // King attack
            if (wAttackCount >= 2 && wQueens != 0)
                result += Weights.SafetyTable[wAttackWeight];

            if (bAttackCount >= 2 && bQueens != 0)
                result -= Weights.SafetyTable[bAttackWeight];

            // King Shield
            int kingFile;
            const ulong WKShield1 = Bitboard.F2 | Bitboard.G2 | Bitboard.H2;
            const ulong WKShield2 = Bitboard.F3 | Bitboard.G3 | Bitboard.H3;
            const ulong WQShield1 = Bitboard.A2 | Bitboard.B2 | Bitboard.C2;
            const ulong WQShield2 = Bitboard.A3 | Bitboard.B3 | Bitboard.C3;
            const ulong BKShield1 = Bitboard.F7 | Bitboard.G7 | Bitboard.H7;
            const ulong BKShield2 = Bitboard.F6 | Bitboard.G6 | Bitboard.H6;
            const ulong BQShield1 = Bitboard.A7 | Bitboard.B7 | Bitboard.C7;
            const ulong BQShield2 = Bitboard.A6 | Bitboard.B6 | Bitboard.C6;

            kingFile = wKingIndex & 7;
            if (kingFile > 5)
            {
                midValue += Weights.Shield1 * BitOperations.PopCount(wPawns & WKShield1)
                          + Weights.Shield2 * BitOperations.PopCount(wPawns & WKShield2);
            }
            else if (kingFile < 3)
            {
                midValue += Weights.Shield1 * BitOperations.PopCount(wPawns & WQShield1)
                          + Weights.Shield2 * BitOperations.PopCount(wPawns & WQShield2);
            }

            kingFile = bKingIndex & 7;
            if (kingFile > 5)
            {
                midValue -= Weights.Shield1 * BitOperations.PopCount(bPawns & BKShield1)
                          + Weights.Shield2 * BitOperations.PopCount(bPawns & BKShield2);
            }
            else if (kingFile < 3)
            {
                midValue -= Weights.Shield1 * BitOperations.PopCount(bPawns & BQShield1)
                          + Weights.Shield2 * BitOperations.PopCount(bPawns & BQShield2);
            }

            // キャスリングしていないキングがルークをブロックしているときにペナルティ
            if ((wKing  & (Bitboard.F1 | Bitboard.G1)) != 0 && (wRooks & (Bitboard.H1 | Bitboard.G1)) != 0)
                result -= Weights.KingBlocksRook;
            
            if ((wKing  & (Bitboard.C1 | Bitboard.B1)) != 0 && (wRooks & (Bitboard.A1 | Bitboard.B1)) != 0)
                result -= Weights.KingBlocksRook;
            
            if ((bKing  & (Bitboard.F8 | Bitboard.G8)) != 0 && (bRooks & (Bitboard.H8 | Bitboard.G8)) != 0)
                result += Weights.KingBlocksRook;
            
            if ((bKing  & (Bitboard.C8 | Bitboard.B8)) != 0 && (bRooks & (Bitboard.A8 | Bitboard.B8)) != 0)
                result += Weights.KingBlocksRook;

            /******************************
             *  Add all evaluation terms  *
             ******************************/
            int phase = ((Math.Min(board.State.Phases[0] + board.State.Phases[1], 24) * 256) + 12) / 24;
            result += (midValue * phase + endValue * (256 - phase)) / 256;
            
            /*****************************
             *  Low material correction  *
             *****************************/
            int stronger = result > 0 ? Color.White : Color.Black;
            if (board.Pieces[stronger][PieceType.Pawn] == 0)
            {
                int weaker = stronger ^ 1;

                // マイナーピース単体では勝てない
                if (board.State.Phases[stronger] <= 1) return 0;

                // K vs KNN はドロー
                if (board.Pieces[weaker][PieceType.Pawn] == 0
                 && BitOperations.PopCount(board.Pieces[stronger][PieceType.Knight]) == 2)
                    return 0;

                // マテリアル差がマイナーピース１個分の場合はほぼドロー
                if (board.State.Phases[stronger] < 4
                 && board.State.Phases[stronger] - board.State.Phases[weaker] == 1)
                    result >>= 4;
            }

            return (1 - (board.Side << 1)) * result;
        }

        public static int FastEvaluate(Board board)
        {
            int phase = (Math.Min(24, board.State.Phases[0] + board.State.Phases[1]) * 256 + 12) / 24;
            int value = (board.State.MidgameValue * phase + board.State.EndgameValue * (256 - phase)) / 256;

            return (1 - (board.Side << 1)) * value;
        }

        public static void Clear()
        {
            PawnHashTable.Clear();
        }

        public static void Print(Board board)
        {
            int    sign   = board.Side == Color.White ? 1 : -1;
            int    phase  = Math.Min(24, board.State.Phases[0] + board.State.Phases[1]);
            double value  = 0.01 * sign * Evaluate(board);
            double mgPcsq = 0.01 * board.State.MidgameValue * (phase / 24.0);
            double egPcsq = 0.01 * board.State.EndgameValue * ((24 - phase) / 24.0);
            double pawn   = 0.01 * PawnHashTable.Read(board);
            double tempo  = 0.01 * sign * Weights.Tempo;
            double pcsq   = mgPcsq + egPcsq;
            double others = value - (pcsq + pawn + tempo);

            Console.WriteLine($"\nTotal value: {value:F2}");
            Console.WriteLine( "+----------------+--------+");
            Console.WriteLine( "|      Term      |  Value |");
            Console.WriteLine( "+----------------+--------+");
            Console.WriteLine($"| Game phase     | {100 * phase / 24,5}% |");
            Console.WriteLine($"| Pcsq table     | {pcsq,  6:F2} |");
            Console.WriteLine($"| Pawn structure | {pawn,  6:F2} |");
            Console.WriteLine($"| Tempo          | {tempo, 6:F2} |");
            Console.WriteLine($"| Others         | {others,6:F2} |");
            Console.WriteLine( "+----------------+--------+\n");
        }
    }
}
