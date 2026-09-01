using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    public static class MoveGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GenerateLegalMoves(ref MoveBuffer buffer, Board board)
        {
            if (board.IsWhiteToMove)
            {
                MoveGenerator<White>.GenerateLegalMoves(ref buffer, board, board.IsInCheck<White>());
            }
            else
            {
                MoveGenerator<Black>.GenerateLegalMoves(ref buffer, board, board.IsInCheck<Black>());
            }
        }
    }

    internal static class MoveGenerator<TColor> where TColor : struct, IColor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GenerateLegalMoves<TMoveBuffer>(ref TMoveBuffer buffer, Board board, bool isInCheck)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            if (isInCheck) { GenerateEvasionMoves(ref buffer, board); }
            else           { GenerateNonEvasionMoves(ref buffer, board); }
        }

        internal static void GenerateEvasionMoves<TMoveBuffer>(ref TMoveBuffer buffer, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            int   kingIndex = board.GetKingIndex<TColor>();
            ulong checkers  = CalculateCheckers(kingIndex, board);
            ulong targets   = ~board.GetFriends<TColor>() & Bitboard.GetBetweenSquares(included: Bitboard.GetLsb(checkers), excluded: kingIndex);
            ulong occupancy = board.Occupancy;
            ulong empties   = ~occupancy;
            ulong enemies   = board.GetEnemies<TColor>();

            if (!Bitboard.HasMultipleBits(checkers))
            {
                ulong pinnedPieces = GeneratePinnedMoves(ref buffer, kingIndex, empties, enemies, targets, board);

                ulong pawns = board.GetFriends<TColor>(PieceType.Pawn) & ~pinnedPieces;
                ulong pawnsNotRank7 = pawns & ~TColor.Rank7;
                GeneratePawnPushMoves(ref buffer, pawnsNotRank7, empties, targets);

                ulong enemyTargets = enemies & targets;
                GeneratePawnCaptureMoves(ref buffer, pawnsNotRank7, enemyTargets);
                GenerateEnPassantMoves(ref buffer, pawns, kingIndex, enemyTargets, targets, board);

                ulong pawnsRank7 = pawns & TColor.Rank7;
                GeneratePushPromotionMoves(ref buffer, pawnsRank7, empties, targets);
                GenerateCapturePromotionMoves(ref buffer, pawnsRank7, enemyTargets);

                GeneratePieceMoves(ref buffer, occupancy, targets, pinnedPieces, board);
            }

            occupancy ^= 1UL << kingIndex;
            ulong attacks = Attacks.GetKingAttacks(kingIndex) & ~board.GetFriends<TColor>() & ~board.CalculateAttackedBB<TColor>(occupancy);
            while (attacks != 0) { buffer.Add(new Move(kingIndex, Bitboard.PopLsb(ref attacks))); }
        }

        internal static void GenerateNonEvasionMoves<TMoveBuffer>(ref TMoveBuffer buffer, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong targets   = ~board.GetFriends<TColor>();
            ulong occupancy = board.Occupancy;
            ulong empties   = ~occupancy;
            ulong enemies   = board.GetEnemies<TColor>();
            int   kingIndex = board.GetKingIndex<TColor>();
            
            ulong pinnedPieces = GeneratePinnedMoves(ref buffer, kingIndex, empties, enemies, targets, board);

            ulong pawns = board.GetFriends<TColor>(PieceType.Pawn) & ~pinnedPieces;
            ulong pawnsNotRank7 = pawns & ~TColor.Rank7;
            GeneratePawnPushMoves(ref buffer, pawnsNotRank7, empties, targets);

            ulong enemyTargets = enemies & targets;
            GeneratePawnCaptureMoves(ref buffer, pawnsNotRank7, enemyTargets);
            GenerateEnPassantMoves(ref buffer, pawns, kingIndex, enemyTargets, targets, board);

            ulong pawnsRank7 = pawns & TColor.Rank7;
            GeneratePushPromotionMoves(ref buffer, pawnsRank7, empties, targets);
            GenerateCapturePromotionMoves(ref buffer, pawnsRank7, enemyTargets);

            GeneratePieceMoves(ref buffer, occupancy, targets, pinnedPieces, board);

            occupancy ^= 1UL << kingIndex;
            ulong attackedBB = board.CalculateAttackedBB<TColor>(occupancy);
            GenerateCastlingMoves(ref buffer, attackedBB, board);
            ulong attacks = Attacks.GetKingAttacks(kingIndex) & ~board.GetFriends<TColor>() & ~attackedBB;
            while (attacks != 0) { buffer.Add(new Move(kingIndex, Bitboard.PopLsb(ref attacks))); }
        }

        internal static void GenerateTacticalMoves<TMoveBuffer>(ref TMoveBuffer buffer, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong occupancy = board.Occupancy;
            ulong empties   = ~occupancy;
            ulong enemies   = board.GetEnemies<TColor>();
            int   kingIndex = board.GetKingIndex<TColor>();

            ulong pinnedPieces = GeneratePinnedMoves(ref buffer, kingIndex, empties, enemies, targets: enemies, board);

            ulong pawns = board.GetFriends<TColor>(PieceType.Pawn) & ~pinnedPieces;
            ulong pawnsNotRank7 = pawns & ~TColor.Rank7;
            GeneratePawnCaptureMoves(ref buffer, pawnsNotRank7, enemies);
            GenerateEnPassantMoves(ref buffer, pawns, kingIndex, checker: 0UL, targets: enemies, board);

            ulong pawnsRank7 = pawns & TColor.Rank7;
            ulong pushPromotions = TColor.GetSinglePawnPush(pawnsRank7) & empties;
            while (pushPromotions != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref pushPromotions);
                int originIndex = targetIndex - TColor.Up;
                buffer.Add(new Move(MoveType.QueenPromotion, originIndex, targetIndex));
            }
            GenerateCapturePromotionMoves(ref buffer, pawnsRank7, enemies);

            GeneratePieceMoves(ref buffer, occupancy, targets: enemies, pinnedPieces, board);

            occupancy ^= 1UL << kingIndex;
            ulong attacks = Attacks.GetKingAttacks(kingIndex) & enemies & ~board.CalculateAttackedBB<TColor>(occupancy);
            while (attacks != 0) { buffer.Add(new Move(kingIndex, Bitboard.PopLsb(ref attacks))); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GeneratePinnedMoves<TMoveBuffer>(ref TMoveBuffer buffer, int kingIndex, ulong empties, ulong enemies, ulong targets, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong ray = Attacks.GetBishopAttacks(kingIndex, enemies);
            ulong pinners = ray & (board.GetEnemies<TColor>(PieceType.Queen) | board.GetEnemies<TColor>(PieceType.Bishop));
            ulong friends = board.GetFriends<TColor>();
            ulong pinnedPieces = 0UL;
            while (pinners != 0)
            {
                ulong pinnedLine = ray & Bitboard.GetBetweenSquares(included: Bitboard.PopLsb(ref pinners), excluded: kingIndex);
                ulong blockers = friends & pinnedLine;
                if (Bitboard.HasSingleBit(blockers))
                {
                    pinnedPieces |= blockers;

                    int originIndex = Bitboard.GetLsb(blockers);
                    switch (board.GetPieceType(originIndex))
                    {
                        case PieceType.Pawn:
                            if ((blockers & TColor.Rank7) == 0)
                            {
                                GeneratePawnCaptureMoves(ref buffer, blockers, enemies & pinnedLine & targets);
                            }
                            else
                            {
                                GenerateCapturePromotionMoves(ref buffer, blockers, enemies & pinnedLine & targets);
                            }
                            break;

                        case PieceType.Bishop:
                        case PieceType.Queen:
                            ulong attacks = (pinnedLine ^ blockers) & targets;
                            while (attacks != 0)
                            {
                                buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks)));
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            ray = Attacks.GetRookAttacks(kingIndex, enemies);
            pinners = ray & (board.GetEnemies<TColor>(PieceType.Queen) | board.GetEnemies<TColor>(PieceType.Rook));
            while (pinners != 0)
            {
                ulong pinnedLine = ray & Bitboard.GetBetweenSquares(included: Bitboard.PopLsb(ref pinners), excluded: kingIndex);
                ulong blockers = friends & pinnedLine;
                if (Bitboard.HasSingleBit(blockers))
                {
                    pinnedPieces |= blockers;

                    int originIndex = Bitboard.GetLsb(blockers);
                    switch (board.GetPieceType(originIndex))
                    {
                        case PieceType.Pawn:
                            GeneratePawnPushMoves(ref buffer, blockers, empties, pinnedLine & targets);
                            break;

                        case PieceType.Rook:
                        case PieceType.Queen:
                            ulong attacks = (pinnedLine ^ blockers) & targets;
                            while (attacks != 0)
                            {
                                buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks)));
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            return pinnedPieces;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePieceMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong occupancy, ulong targets, ulong pinnedPieces, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong pieces = board.GetFriends<TColor>(PieceType.Knight) & ~pinnedPieces;
            while (pieces != 0)
            {
                int originIndex = Bitboard.PopLsb(ref pieces);
                ulong attacks = Attacks.GetKnightAttacks(originIndex) & targets;
                while (attacks != 0) { buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks))); }
            }

            pieces = board.GetFriends<TColor>(PieceType.Bishop) & ~pinnedPieces;
            while (pieces != 0)
            {
                int originIndex = Bitboard.PopLsb(ref pieces);
                ulong attacks = Attacks.GetBishopAttacks(originIndex, occupancy) & targets;
                while (attacks != 0) { buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks))); }
            }

            pieces = board.GetFriends<TColor>(PieceType.Rook) & ~pinnedPieces;
            while (pieces != 0)
            {
                int originIndex = Bitboard.PopLsb(ref pieces);
                ulong attacks = Attacks.GetRookAttacks(originIndex, occupancy) & targets;
                while (attacks != 0) { buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks))); }
            }

            pieces = board.GetFriends<TColor>(PieceType.Queen) & ~pinnedPieces;
            while (pieces != 0)
            {
                int originIndex = Bitboard.PopLsb(ref pieces);
                ulong attacks = Attacks.GetQueenAttacks(originIndex, occupancy) & targets;
                while (attacks != 0) { buffer.Add(new Move(originIndex, Bitboard.PopLsb(ref attacks))); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePawnPushMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong pawns, ulong empties, ulong targets)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong singlePushes = TColor.GetSinglePawnPush(pawns) & empties;
            ulong doublePushes = TColor.GetSinglePawnPush(singlePushes) & empties & targets & TColor.Rank4;
            
            singlePushes &= targets;
            while (singlePushes != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref singlePushes);
                buffer.Add(new Move(targetIndex - TColor.Up, targetIndex));
            }
            while (doublePushes != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref doublePushes);
                buffer.Add(new Move(targetIndex - (TColor.Up + TColor.Up), targetIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePawnCaptureMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong pawns, ulong enemyTargets)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong rightCaptures = TColor.GetPawnRightAttacks(pawns) & enemyTargets;
            while (rightCaptures != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref rightCaptures);
                buffer.Add(new Move(targetIndex - TColor.UpRight, targetIndex));
            }

            ulong leftCaptures = TColor.GetPawnLeftAttacks(pawns) & enemyTargets;
            while (leftCaptures != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref leftCaptures);
                buffer.Add(new Move(targetIndex - TColor.UpLeft, targetIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePushPromotionMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong pawns, ulong empties, ulong targets)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong promotions = TColor.GetSinglePawnPush(pawns) & empties & targets;
            while (promotions != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref promotions);
                int originIndex = targetIndex - TColor.Up;
                buffer.Add(new Move(MoveType.RookPromotion,   originIndex, targetIndex));
                buffer.Add(new Move(MoveType.BishopPromotion, originIndex, targetIndex));
                buffer.Add(new Move(MoveType.QueenPromotion,  originIndex, targetIndex));
                buffer.Add(new Move(MoveType.KnightPromotion, originIndex, targetIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateCapturePromotionMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong pawns, ulong enemyTargets)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            ulong rightPromotions = TColor.GetPawnRightAttacks(pawns) & enemyTargets;
            while (rightPromotions != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref rightPromotions);
                int originIndex = targetIndex - TColor.UpRight;
                buffer.Add(new Move(MoveType.RookPromotion,   originIndex, targetIndex));
                buffer.Add(new Move(MoveType.BishopPromotion, originIndex, targetIndex));
                buffer.Add(new Move(MoveType.QueenPromotion,  originIndex, targetIndex));
                buffer.Add(new Move(MoveType.KnightPromotion, originIndex, targetIndex));
            }

            ulong leftPromotions = TColor.GetPawnLeftAttacks(pawns) & enemyTargets;
            while (leftPromotions != 0)
            {
                int targetIndex = Bitboard.PopLsb(ref leftPromotions);
                int originIndex = targetIndex - TColor.UpLeft;
                buffer.Add(new Move(MoveType.RookPromotion,   originIndex, targetIndex));
                buffer.Add(new Move(MoveType.BishopPromotion, originIndex, targetIndex));
                buffer.Add(new Move(MoveType.QueenPromotion,  originIndex, targetIndex));
                buffer.Add(new Move(MoveType.KnightPromotion, originIndex, targetIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateEnPassantMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong pawns, int kingIndex, ulong checker, ulong targets, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            int enPassantIndex = board.EnPassantIndex;
            if (enPassantIndex != Square.None)
            {
                if (checker != 0
                 && ((1UL << enPassantIndex) & targets) == 0
                 && ((1UL << (enPassantIndex - TColor.Up)) & checker) == 0)
                {
                    return;
                }

                ulong enPassants = pawns & (TColor.IsWhite ? Attacks.GetPawnAttacks<Black>(enPassantIndex)
                                                           : Attacks.GetPawnAttacks<White>(enPassantIndex));
                while (enPassants != 0)
                {
                    int originIndex = Bitboard.PopLsb(ref enPassants);
                    ulong occupancy = board.Occupancy ^ (1UL << originIndex) ^ (1UL << enPassantIndex) ^ (1UL << (enPassantIndex - TColor.Up));
                    if ((Attacks.GetRookAttacks(kingIndex, occupancy)
                      & (board.GetEnemies<TColor>(PieceType.Queen) | board.GetEnemies<TColor>(PieceType.Rook))) == 0)
                    {
                        buffer.Add(new Move(MoveType.EnPassant, originIndex, enPassantIndex));
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateCastlingMoves<TMoveBuffer>(ref TMoveBuffer buffer, ulong attackedBB, Board board)
            where TMoveBuffer : struct, IMoveBuffer, allows ref struct
        {
            int castlingRights = board.CastlingRights;
            if (board.CanCastleKingside<TColor>(castlingRights, attackedBB))
            {
                buffer.Add(TColor.IsWhite ? new Move(MoveType.O_O, Square.E1, Square.H1) : new Move(MoveType.O_O, Square.E8, Square.H8));
            }
            if (board.CanCastleQueenside<TColor>(castlingRights, attackedBB))
            {
                buffer.Add(TColor.IsWhite ? new Move(MoveType.O_O_O, Square.E1, Square.A1) : new Move(MoveType.O_O_O, Square.E8, Square.A8));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong CalculateCheckers(int kingIndex, Board board)
        {
            ulong occupancy = board.Occupancy;
            return (board.GetEnemies<TColor>(PieceType.Queen)  & Attacks.GetQueenAttacks(kingIndex, occupancy))
                 | (board.GetEnemies<TColor>(PieceType.Rook)   & Attacks.GetRookAttacks(kingIndex, occupancy))
                 | (board.GetEnemies<TColor>(PieceType.Bishop) & Attacks.GetBishopAttacks(kingIndex, occupancy))
                 | (board.GetEnemies<TColor>(PieceType.Knight) & Attacks.GetKnightAttacks(kingIndex))
                 | (board.GetEnemies<TColor>(PieceType.Pawn)   & Attacks.GetPawnAttacks<TColor>(kingIndex));
        }
    }
}
