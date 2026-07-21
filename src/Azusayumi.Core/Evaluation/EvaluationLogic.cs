using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Evaluation
{
    internal static class Evaluator<TContext> where TContext : struct, IEvaluationContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TContext Evaluate(Board board, TContext context)
        {
            ulong whitePieces = board.GetFriends<White>();
            ulong blackPieces = board.GetFriends<Black>();
            ulong occupancy   = board.Occupancy;

            ulong queens = board.GetFriends<White>(PieceType.Queen);
            while (queens != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref queens);
                int mobility    = Bitboard.PopCount(Attacks.GetQueenAttacks(squareIndex, occupancy) & ~whitePieces);
                context.Add<White>(Term.QueenMobility, index: mobility);
            }

            queens = board.GetFriends<Black>(PieceType.Queen);
            while (queens != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref queens);
                int mobility    = Bitboard.PopCount(Attacks.GetQueenAttacks(squareIndex, occupancy) & ~blackPieces);
                context.Add<Black>(Term.QueenMobility, index: mobility);
            }

            ulong rooks = board.GetFriends<White>(PieceType.Rook);
            while (rooks != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref rooks);
                int mobility    = Bitboard.PopCount(Attacks.GetRookAttacks(squareIndex, occupancy) & ~whitePieces);
                context.Add<White>(Term.RookMobility, index: mobility);
            }

            rooks = board.GetFriends<Black>(PieceType.Rook);
            while (rooks != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref rooks);
                int mobility    = Bitboard.PopCount(Attacks.GetRookAttacks(squareIndex, occupancy) & ~blackPieces);
                context.Add<Black>(Term.RookMobility, index: mobility);
            }

            ulong bishops = board.GetFriends<White>(PieceType.Bishop);
            while (bishops != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref bishops);
                int mobility    = Bitboard.PopCount(Attacks.GetBishopAttacks(squareIndex, occupancy) & ~whitePieces);
                context.Add<White>(Term.BishopMobility, index: mobility);
            }

            bishops = board.GetFriends<Black>(PieceType.Bishop);
            while (bishops != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref bishops);
                int mobility    = Bitboard.PopCount(Attacks.GetBishopAttacks(squareIndex, occupancy) & ~blackPieces);
                context.Add<Black>(Term.BishopMobility, index: mobility);
            }

            ulong knights = board.GetFriends<White>(PieceType.Knight);
            while (knights != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref knights);
                int mobility    = Bitboard.PopCount(Attacks.GetKnightAttacks(squareIndex) & ~whitePieces);
                context.Add<White>(Term.KnightMobility, index: mobility);
            }

            knights = board.GetFriends<Black>(PieceType.Knight);
            while (knights != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref knights);
                int mobility    = Bitboard.PopCount(Attacks.GetKnightAttacks(squareIndex) & ~blackPieces);
                context.Add<Black>(Term.KnightMobility, index: mobility);
            }

            return context;
        }

        internal static TContext EvaluatePst(Board board, TContext context)
        {
            ulong whitePieces = board.GetFriends<White>();
            while (whitePieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref whitePieces);
                int pieceType   = board.GetPieceType(squareIndex);

                if (pieceType != PieceType.King)
                {
                    context.Add<White>(Term.Material, index: pieceType);
                }
                context.Add<White>(Term.PawnPst + pieceType, index: squareIndex ^ 56);
            }

            ulong blackPieces = board.GetFriends<Black>();
            while (blackPieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref blackPieces);
                int pieceType   = board.GetPieceType(squareIndex);

                if (pieceType != PieceType.King)
                {
                    context.Add<Black>(Term.Material, index: pieceType);
                }
                context.Add<Black>(Term.PawnPst + pieceType, index: squareIndex);
            }

            return context;
        }
    }
}
