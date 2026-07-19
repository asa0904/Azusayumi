using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Tests
{
    internal static class BoardExtensions
    {
        internal static Move ToMove(this Board board, string stringMove)
        {
            MoveBuffer buffer = new(stackalloc Move[256]);
            MoveGenerator.GenerateLegalMoves(ref buffer, board);

            Span<Move> moves = buffer.AsSpan();
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].ToString() == stringMove) { return moves[i]; }
            }

            throw new ArgumentException($"'{stringMove}' is illegal.");
        }
    }
}
