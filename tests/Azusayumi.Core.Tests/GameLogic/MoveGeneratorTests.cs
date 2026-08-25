using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Tests.GameLogic
{
    public class MoveGeneratorTests
    {
        public static TheoryData<string> GetMoveGeneratorTestData()
        {
            return new([
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1",
                "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1",
                "1q2k3/2P5/8/8/8/8/2p5/1Q2K3 w - - 0 1",
                "1q2k3/2P5/8/8/8/8/2p5/1Q2K3 b - - 0 1",
                "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",
                "4k3/8/8/8/3Pp3/8/8/4K3 b - d3 0 1",
            ]);
        }

        [Theory]
        [MemberData(nameof(GetMoveGeneratorTestData))]
        public void GenerateTacticalMoves_MatchesFilteredLegalMoves(string fen)
        {
            Board board = new(fen, historyCapacity: 2);

            MoveBuffer expectedBuffer = new(stackalloc Move[256]);
            MoveGenerator.GenerateLegalMoves(ref expectedBuffer, board);
            Span<Move> expected = FilterTacticalMoves(expectedBuffer.AsSpan(), board);

            MoveBuffer actualBuffer = new(stackalloc Move[32]);
            if (board.IsWhiteToMove) { MoveGenerator<White>.GenerateTacticalMoves(ref actualBuffer, board); }
            else                     { MoveGenerator<Black>.GenerateTacticalMoves(ref actualBuffer, board); }
            Span<Move> actual = actualBuffer.AsSpan();
            
            Assert.True(expected.Length == actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(actual.Contains(expected[i]));
            }
        }

        private static Span<Move> FilterTacticalMoves(Span<Move> moves, Board board)
        {
            int count = 0;
            foreach (Move move in moves)
            {
                if (IsTacticalMove(move, board)) { moves[count++] = move; }
            }

            return moves[..count];
        }

        private static bool IsTacticalMove(Move move, Board board)
        {
            int moveType = move.Type;
            return (board.GetPieceType(move.TargetIndex) != PieceType.None && moveType != MoveType.Castling)
                || (moveType == MoveType.Promotion && move.PromotionType == PieceType.Queen)
                || moveType == MoveType.EnPassant;
        }
    }
}
