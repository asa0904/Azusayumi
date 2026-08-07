using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal record struct ScoredMove(Move Move, short Score);

    internal class MoveOrdering
    {
        private const int Quiet = 0;

        private const int Kx = Quiet + 1;
        private const int Qx = Kx + 1;
        private const int Rx = Kx + 2;
        private const int Bx = Kx + 3;
        private const int Nx = Kx + 4;
        private const int Px = Kx + 5;

        private const int P = 0;
        private const int N = 1 * PieceType.Length;
        private const int B = 2 * PieceType.Length;
        private const int R = 3 * PieceType.Length;
        private const int Q = 4 * PieceType.Length;

        private static readonly short[] _mvvLvaScores =
        [
            Px+P, Px+N, Px+B, Px+R, Px+Q, Quiet, Quiet, Quiet,
            Nx+P, Nx+N, Nx+B, Nx+R, Nx+Q, Quiet, Quiet, Quiet,
            Bx+P, Bx+N, Bx+B, Bx+R, Bx+Q, Quiet, Quiet, Quiet,
            Rx+P, Rx+N, Rx+B, Rx+R, Rx+Q, Quiet, Quiet, Quiet,
            Qx+P, Qx+N, Qx+B, Qx+R, Qx+Q, Quiet, Quiet, Quiet,
            Kx+P, Kx+N, Kx+B, Kx+R, Kx+Q, Quiet, Quiet, Quiet,
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ScoreCaptures(Span<ScoredMove> scoredMoves, Board board)
        {
            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = scoredMoves[i].Move;
                if (move.Type == MoveType.EnPassant)
                {
                    scoredMoves[i].Score = Px + P;
                }
                else
                {
                    int victim   = board.GetPieceType(move.TargetIndex);
                    int attacker = board.GetPieceType(move.OriginIndex);
                    scoredMoves[i].Score = _mvvLvaScores[(attacker << 3) | victim];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Move Select(int index, Span<ScoredMove> scoredMoves)
        {
            int bestIndex = index;
            for (int i = index + 1; i < scoredMoves.Length; i++)
            {
                if (scoredMoves[i].Score > scoredMoves[bestIndex].Score)
                {
                    bestIndex = i;
                }
            }

            if (bestIndex != index)
            {
                ScoredMove bestMove = scoredMoves[bestIndex];
                scoredMoves[bestIndex] = scoredMoves[index];
                scoredMoves[index] = bestMove;
            }

            return scoredMoves[index].Move;
        }
    }
}
