using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Tests.GameLogic
{
    public class PerftTests
    {
        [Theory]
        [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 6, 119060324)]
        [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 5, 193690690)]
        public void Perft(string fen, int depth, long expectedNodes)
        {
            Board board = new(fen, historyCapacity: depth + 1);

            long actualNodes = CountNodes(board, depth);

            Assert.Equal(expectedNodes, actualNodes);
        }

        private static long CountNodes(Board board, int depth)
        {
            MoveBuffer buffer = new(stackalloc Move[256]);
            MoveGenerator.GenerateLegalMoves(ref buffer, board);
            Span<Move> moves = buffer.AsSpan();
            if (depth == 1)
            {
                return moves.Length;
            }

            long nodes = 0L;
            if (board.IsWhiteToMove)
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    Move move = moves[i];
                    board.MakeMove<White>(move);
                    nodes += CountNodes(board, depth - 1);
                    board.UnmakeMove<White>(move);
                }
            }
            else
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    Move move = moves[i];
                    board.MakeMove<Black>(move);
                    nodes += CountNodes(board, depth - 1);
                    board.UnmakeMove<Black>(move);
                }
            }

            return nodes;
        }
    }
}
