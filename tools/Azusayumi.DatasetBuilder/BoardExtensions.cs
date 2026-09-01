using Azusayumi.Core.GameLogic;

namespace Azusayumi.DatasetBuilder
{
    internal static class BoardExtensions
    {
        internal static bool TryParse(this Board board, string notation, out Move move)
        {
            return board.IsWhiteToMove ? board.TryParse<White>(notation, out move)
                                       : board.TryParse<Black>(notation, out move);
        }

        private static bool TryParse<TColor>(this Board board, string notation, out Move move)
            where TColor : struct, IColor
        {
            move = Move.Null;

            if (notation.SequenceEqual("0000")) { return false; }

            MoveBuffer buffer = new(stackalloc Move[256]);
            MoveGenerator.GenerateLegalMoves(ref buffer, board);
            Span<Move> moves = buffer.AsSpan();

            notation = notation.Replace("+", null).Replace("#", null);

            if (notation == "O-O")
            {
                move = TColor.IsWhite ? new Move(MoveType.O_O, Square.E1, Square.H1)
                                      : new Move(MoveType.O_O, Square.E8, Square.H8);
                return moves.Contains(move);
            }
            else if (notation == "O-O-O")
            {
                move = TColor.IsWhite ? new Move(MoveType.O_O_O, Square.E1, Square.A1)
                                      : new Move(MoveType.O_O_O, Square.E8, Square.A8);
                return moves.Contains(move);
            }

            int rank, file;
            int originIndex, targetIndex;
            
            // Pawn
            if (notation[0] is >= 'a' and <= 'h')
            {
                int moveType = MoveType.Normal;

                // Promotion
                if (notation.Contains('='))
                {
                    moveType = notation[^1] switch
                    {
                        'N' => MoveType.KnightPromotion,
                        'B' => MoveType.BishopPromotion,
                        'R' => MoveType.RookPromotion,
                        'Q' => MoveType.QueenPromotion,
                        _   => MoveType.Normal,
                    };
                    notation = notation[..^2];
                }

                rank = notation[^1] - '1';
                file = notation[^2] - 'a';
                targetIndex = Square.GetIndex(rank, file);

                if (targetIndex == board.EnPassantIndex)
                {
                    moveType = MoveType.EnPassant;
                }

                ulong pawns = board.GetFriends<TColor>(PieceType.Pawn);

                // Capture
                if (notation.Contains('x'))
                {
                    pawns &= GetMask(notation[0]);
                    pawns &= TColor.IsWhite ? Attacks.GetPawnAttacks<Black>(targetIndex)
                                            : Attacks.GetPawnAttacks<White>(targetIndex);
                    originIndex = Bitboard.GetLsb(pawns);
                }
                else
                {
                    // Single push
                    originIndex = targetIndex - TColor.Up;

                    // Double push
                    if (((1UL << originIndex) & pawns) == 0)
                    {
                        originIndex -= TColor.Up;
                    }
                }
                
                move = new Move(moveType, originIndex, targetIndex);
                return moves.Contains(move);
            }

            rank = notation[^1] - '1';
            file = notation[^2] - 'a';
            targetIndex = Square.GetIndex(rank, file);
            
            int pieceType = GetPieceType(notation[0]);
            ulong pieces = board.GetFriends<TColor>(pieceType) & GetAttacks(pieceType, targetIndex, board.Occupancy);

            // Disambiguating moves
            for (int i = 1; i < notation.Length - 2; i++)
            {
                pieces &= GetMask(notation[i]);
            }

            // The ambiguity is resolved when the other piece is pinned.
            if (Bitboard.HasMultipleBits(pieces))
            {
                while (pieces != 0)
                {
                    move = new(Bitboard.PopLsb(ref pieces), targetIndex);
                    if (moves.Contains(move)) { return true; }
                }
            }
            
            move = new Move(Bitboard.GetLsb(pieces), targetIndex);
            return moves.Contains(move);
        }

        private static int GetPieceType(char symbol)
        {
            return symbol switch
            {
                'N' => PieceType.Knight,
                'B' => PieceType.Bishop,
                'R' => PieceType.Rook,
                'Q' => PieceType.Queen,
                'K' => PieceType.King,
                _   => PieceType.None,
            };
        }

        private static ulong GetMask(char symbol)
        {
            return symbol switch
            {
                '1' => Bitboard.Rank1,
                '2' => Bitboard.Rank2,
                '3' => Bitboard.Rank3,
                '4' => Bitboard.Rank4,
                '5' => Bitboard.Rank5,
                '6' => Bitboard.Rank6,
                '7' => Bitboard.Rank7,
                '8' => Bitboard.Rank8,
                'a' => Bitboard.FileA,
                'b' => Bitboard.FileB,
                'c' => Bitboard.FileC,
                'd' => Bitboard.FileD,
                'e' => Bitboard.FileE,
                'f' => Bitboard.FileF,
                'g' => Bitboard.FileG,
                'h' => Bitboard.FileH,
                _ => ~0UL, // 'x'
            };
        }

        private static ulong GetAttacks(int pieceType, int squareIndex, ulong occupancy)
        {
            return pieceType switch
            {
                PieceType.Knight => Attacks.GetKnightAttacks(squareIndex),
                PieceType.Bishop => Attacks.GetBishopAttacks(squareIndex, occupancy),
                PieceType.Rook   => Attacks.GetRookAttacks(squareIndex, occupancy),
                PieceType.Queen  => Attacks.GetQueenAttacks(squareIndex, occupancy),
                PieceType.King   => Attacks.GetKingAttacks(squareIndex),
                _ => 0UL
            };
        }
    }
}
