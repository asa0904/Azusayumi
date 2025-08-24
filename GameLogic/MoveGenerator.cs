using System.Numerics;

namespace Azusayumi.GameLogic
{
    internal static class MoveGenerator
    {
        public static void GenerateAllMoves(MoveList moves, Board board)
        {
            byte  kingIndex = board.State.KingIndex;
            ulong targets   = ~board.PiecesByColor[board.Side];

            GenerateNoneKingMoves(moves, board, kingIndex, targets);

            ulong attackedBoard = board.CalculateAttackedBoard();
            ulong kingAttacks = Attack.KingAttacks[kingIndex] & targets & ~attackedBoard;
            while (kingAttacks > 0) moves.Add(new(kingIndex, Bitboard.PopLSB(ref kingAttacks)));
            GenerateCastleMoves(moves, board, attackedBoard);
        }

        public static void GenerateCaptureMoves(MoveList moves, Board board)
        {
            byte  kingIndex = board.State.KingIndex;
            ulong targets   = board.PiecesByColor[board.Side ^ 1];

            GenerateNoneKingMoves(moves, board, kingIndex, targets);

            ulong attackedBoard = board.CalculateAttackedBoard();
            ulong kingAttacks = Attack.KingAttacks[kingIndex] & targets & ~attackedBoard;
            while (kingAttacks > 0) moves.Add(new(kingIndex, Bitboard.PopLSB(ref kingAttacks)));
        }

        public static void GenerateQuietMoves(MoveList moves, Board board)
        {
            byte  kingIndex = board.State.KingIndex;
            ulong targets   = ~board.Occupancy;

            GenerateNoneKingMoves(moves, board, kingIndex, targets);

            ulong attackedBoard = board.CalculateAttackedBoard();
            ulong kingAttacks = Attack.KingAttacks[kingIndex] & targets & ~attackedBoard;
            while (kingAttacks > 0) moves.Add(new(kingIndex, Bitboard.PopLSB(ref kingAttacks)));
            GenerateCastleMoves(moves, board, attackedBoard);
        }

        private static void GenerateNoneKingMoves(MoveList moves, Board board, byte kingIndex, ulong targets)
        {
            ulong occupancy    = board.Occupancy;
            ulong pinnedPieces = board.CalculatePinnedPieces();
            ulong checkers     = board.CalculateCheckers();
            int   checkerCount = BitOperations.PopCount(checkers);

            if (checkerCount <= 1)
            {
                if (checkerCount == 1)
                    targets &= checkers | Bitboard.BetweenLines[kingIndex][BitOperations.TrailingZeroCount(checkers)];

                // Pawn
                ulong pawns = board.Pieces[board.Side][PieceType.Pawn];
                ulong pinnedPawns = pawns & pinnedPieces;
                while (pinnedPawns > 0)
                {
                    byte pawnIndex = Bitboard.PopLSB(ref pinnedPawns);
                    ulong pinnedLine = Bitboard.Lines[kingIndex][pawnIndex];
                    GeneratePawnMoves(moves, board, 1UL << pawnIndex, targets & pinnedLine);
                }
                GeneratePawnMoves(moves, board, pawns & ~pinnedPieces, targets);
                
                ulong pieces;

                // Knight
                pieces = board.Pieces[board.Side][PieceType.Knight] & ~pinnedPieces;
                while (pieces > 0)
                {
                    byte originIndex = Bitboard.PopLSB(ref pieces);
                    ulong attacks = Attack.KnightAttacks[originIndex] & targets;
                    while (attacks > 0) moves.Add(new Move(originIndex, Bitboard.PopLSB(ref attacks)));
                }

                // Bishop
                pieces = board.Pieces[board.Side][PieceType.Bishop] & ~pinnedPieces;
                while (pieces > 0)
                {
                    byte originIndex = Bitboard.PopLSB(ref pieces);
                    ulong attacks = Attack.GetBishopAttacks(originIndex, occupancy) & targets;
                    while (attacks > 0) moves.Add(new Move(originIndex, Bitboard.PopLSB(ref attacks)));
                }

                // Rook
                pieces = board.Pieces[board.Side][PieceType.Rook] & ~pinnedPieces;
                while (pieces > 0)
                {
                    byte originIndex = Bitboard.PopLSB(ref pieces);
                    ulong attacks = Attack.GetRookAttacks(originIndex, occupancy) & targets;
                    while (attacks > 0) moves.Add(new Move(originIndex, Bitboard.PopLSB(ref attacks)));
                }

                // Queen
                pieces = board.Pieces[board.Side][PieceType.Queen] & ~pinnedPieces;
                while (pieces > 0)
                {
                    byte originIndex = Bitboard.PopLSB(ref pieces);
                    ulong attacks = Attack.GetQueenAttacks(originIndex, occupancy) & targets;
                    while (attacks > 0) moves.Add(new Move(originIndex, Bitboard.PopLSB(ref attacks)));
                }

                // Pinned pieces
                pinnedPieces &= ~pawns;
                while (pinnedPieces > 0)
                {
                    byte originIndex = Bitboard.PopLSB(ref pinnedPieces);
                    byte pieceType = board.PieceTypes[originIndex];
                    ulong attacks = Attack.GetAttacks(pieceType, originIndex, occupancy) & targets & Bitboard.Lines[kingIndex][originIndex];
                    while (attacks > 0) moves.Add(new Move(originIndex, Bitboard.PopLSB(ref attacks)));
                }
            }
        }

        private static void GeneratePawnMoves(MoveList moves, Board board, ulong pawns, ulong targets)
        {
            ulong pushMask    = ~board.Occupancy;
            ulong captureMask = board.PiecesByColor[board.Side ^ 1] & targets;

            int up, upRight, upLeft;
            ulong pawnsOnRank7,  pawnsNotOnRank7;
            ulong singlePushes,  doublePushes;
            ulong rightCaptures, leftCaptures;

            if (board.Side == Color.White)
            {
                up = 8; upRight = 9; upLeft = 7;

                pawnsOnRank7    = pawns &  Bitboard.Rank7;
                pawnsNotOnRank7 = pawns & ~Bitboard.Rank7;

                singlePushes = (pawnsNotOnRank7 << 8) & pushMask;
                doublePushes = (singlePushes    << 8) & pushMask & Bitboard.Rank4 & targets;

                rightCaptures = (pawnsNotOnRank7 << 9) & captureMask & ~Bitboard.FileA;
                leftCaptures  = (pawnsNotOnRank7 << 7) & captureMask & ~Bitboard.FileH;
            }
            else
            {
                up = -8; upRight = -9; upLeft = -7;

                pawnsOnRank7    = pawns &  Bitboard.Rank2;
                pawnsNotOnRank7 = pawns & ~Bitboard.Rank2;

                singlePushes = (pawnsNotOnRank7 >> 8) & pushMask;
                doublePushes = (singlePushes    >> 8) & pushMask & Bitboard.Rank5 & targets;

                rightCaptures = (pawnsNotOnRank7 >> 9) & captureMask & ~Bitboard.FileH;
                leftCaptures  = (pawnsNotOnRank7 >> 7) & captureMask & ~Bitboard.FileA;
            }

            // Push moves
            singlePushes &= targets;
            while (singlePushes > 0)
            {
                byte targetIndex = Bitboard.PopLSB(ref singlePushes);
                moves.Add(new Move((byte)(targetIndex - up), targetIndex));
            }

            while (doublePushes > 0)
            {
                byte targetIndex = Bitboard.PopLSB(ref doublePushes);
                moves.Add(new Move((byte)(targetIndex - up - up), targetIndex));
            }

            // Capture moves
            while (rightCaptures > 0)
            {
                byte targetIndex = Bitboard.PopLSB(ref rightCaptures);
                moves.Add(new Move((byte)(targetIndex - upRight), targetIndex));
            }

            while (leftCaptures > 0)
            {
                byte targetIndex = Bitboard.PopLSB(ref leftCaptures);
                moves.Add(new Move((byte)(targetIndex - upLeft), targetIndex));
            }

            // Promotion moves
            if (pawnsOnRank7 > 0)
            {
                ulong upPromotions, rightPromotions, leftPromotions;

                if (board.Side == Color.White)
                {
                    upPromotions    = (pawnsOnRank7 << 8) & pushMask & targets;
                    rightPromotions = (pawnsOnRank7 << 9) & captureMask & ~Bitboard.FileA;
                    leftPromotions  = (pawnsOnRank7 << 7) & captureMask & ~Bitboard.FileH;
                }
                else
                {
                    upPromotions    = (pawnsOnRank7 >> 8) & pushMask & targets;
                    rightPromotions = (pawnsOnRank7 >> 9) & captureMask & ~Bitboard.FileH;
                    leftPromotions  = (pawnsOnRank7 >> 7) & captureMask & ~Bitboard.FileA;
                }

                while (upPromotions > 0)
                {
                    byte targetIndex = Bitboard.PopLSB(ref upPromotions);
                    byte originIndex = (byte)(targetIndex - up);

                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Queen));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Knight));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Rook));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Bishop));
                }

                while (rightPromotions > 0)
                {
                    byte targetIndex = Bitboard.PopLSB(ref rightPromotions);
                    byte originIndex = (byte)(targetIndex - upRight);

                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Queen));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Knight));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Rook));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Bishop));
                }

                while (leftPromotions > 0)
                {
                    byte targetIndex = Bitboard.PopLSB(ref leftPromotions);
                    byte originIndex = (byte)(targetIndex - upLeft);

                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Queen));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Knight));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Rook));
                    moves.Add(new Move(originIndex, targetIndex).MakePromotion(PieceType.Bishop));
                }
            }

            // En passant moves
            byte enPassantIndex = board.State.EnPassantIndex;

            if (enPassantIndex != Square.None && captureMask != 0)
            {
                if (((1UL << enPassantIndex) & pushMask & targets) != 0
                 || ((1UL << (enPassantIndex - up)) & captureMask) != 0)
                {
                    ulong enPassants = pawns & Attack.PawnAttacks[board.Side ^ 1][enPassantIndex];

                    while (enPassants > 0)
                    {
                        byte originIndex = Bitboard.PopLSB(ref enPassants);

                        if (board.CanEnPassant(originIndex, enPassantIndex, up))
                            moves.Add(new Move(originIndex, enPassantIndex).MakeEnPassant());
                    }
                }
            }
        }

        private static void GenerateCastleMoves(MoveList moves, Board board, ulong attackedBoard)
        {
            if (board.Side == Color.White)
            {
                if (board.CanCastle(Castling.WhiteKingCastle, attackedBoard))
                    moves.Add(Castling.WhiteKingMove);

                if (board.CanCastle(Castling.WhiteQueenCastle, attackedBoard))
                    moves.Add(Castling.WhiteQueenMove);
            }
            else
            {
                if (board.CanCastle(Castling.BlackKingCastle, attackedBoard))
                    moves.Add(Castling.BlackKingMove);

                if (board.CanCastle(Castling.BlackQueenCastle, attackedBoard))
                    moves.Add(Castling.BlackQueenMove);
            }
        }
    }
}