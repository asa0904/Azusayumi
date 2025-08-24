using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal class MovePicker(Board board, Move ttMove, Move killer1, Move killer2, HistoryHeuristic historyHeuristic)
    {
        private enum Stage
        {
            ReturnTT, GenerateCapture, ReturnCapture, ReturnKiller1, ReturnKiller2, GenerateQuiet, ReturnQuiet
        }

        public bool IsKiller = false;
        
        private Stage stage  = Stage.ReturnTT;
        private readonly Move ttMove  = ttMove;
        private readonly Move killer1 = killer1;
        private readonly Move killer2 = killer2;

        private readonly int[]    values = new int[256];
        private readonly MoveList moves  = new();
        private readonly Board    board  = board;
        private readonly HistoryHeuristic historyHeuristic = historyHeuristic;

        private static readonly byte[][] MvvLvaValues =
        [
            [15, 25, 35, 45, 55], // PxP, PxN, PxB, PxR, PxQ
            [14, 24, 34, 44, 54], // NxP, NxN, NxB, NxR, NxQ
            [13, 23, 33, 43, 53], // BxP, BxN, BxB, BxR, BxQ
            [12, 22, 32, 42, 52], // RxP, RxN, RxB, RxR, RxQ
            [11, 21, 31, 41, 51], // QxP, QxN, QxB, QxR, QxQ
            [10, 20, 30, 40, 50], // KxP, KxN, KxB, KxR, KxQ
        ];

        public static void SortCaptures(MoveList moves, Board board)
        {
            if (moves.Count <= 1) return;

            int[] values = new int[moves.Count];

            for (int i = 0; i < values.Length; i++)
            {
                Move move = moves[i];

                if (move.Type == MoveType.EnPassant)
                {
                    values[i] = MvvLvaValues[PieceType.Pawn][PieceType.Pawn];
                    continue;
                }

                byte attacker = board.PieceTypes[move.OriginIndex];
                byte victim   = board.PieceTypes[move.TargetIndex];

                values[i] = MvvLvaValues[attacker][victim];
            }

            for (int i = 0; i < values.Length; i++)
            {
                int bestValue = values[i];

                for (int j = i + 1; j < values.Length; j++)
                {
                    if (values[j] > bestValue)
                    {
                        bestValue = values[j];

                        (values[i], values[j]) = (values[j], values[i]);
                        (moves [i], moves [j]) = (moves [j], moves [i]);
                    }
                }
            }
        }

        public Move Pick()
        {
        top:
            switch (stage)
            {
                case Stage.ReturnTT:
                    stage++;
                    if (ttMove != Move.Null) return ttMove;
                    goto top;

                case Stage.GenerateCapture:
                    MoveGenerator.GenerateCaptureMoves(moves, board);
                    ScoreCaptures();
                    stage++;
                    goto top;

                case Stage.ReturnCapture:
                    while (moves.Count > 0)
                    {
                        Move move = Select(--moves.Count);
                        if (move != ttMove) return move;
                    }
                    stage++;
                    goto top;

                case Stage.ReturnKiller1:
                    stage++;
                    if (killer1 != ttMove && board.IsKillerLegal(killer1)) { IsKiller = true; return killer1; }
                    goto top;

                case Stage.ReturnKiller2:
                    stage++;
                    if (killer2 != ttMove && board.IsKillerLegal(killer2)) { IsKiller = true; return killer2; }
                    goto top;

                case Stage.GenerateQuiet:
                    IsKiller = false;
                    MoveGenerator.GenerateQuietMoves(moves, board);
                    ScoreQuiets();
                    stage++;
                    goto top;

                case Stage.ReturnQuiet:
                    while (moves.Count > 0)
                    {
                        Move move = Select(--moves.Count);
                        if (move != ttMove && move != killer1 && move != killer2) return move;
                    }
                    break;

                default: break;
            }

            return Move.Null;
        }

        private Move Select(int index)
        {
            int  bestValue = values[index];
            Move bestMove  = moves[index];

            for (int i = index - 1; i >= 0; i--)
            {
                if (values[i] > bestValue)
                {
                    bestValue = values[i];
                    bestMove  = moves [i];

                    (values[index], values[i]) = (values[i], values[index]);
                    (moves [index], moves [i]) = (moves [i], moves [index]);
                }
            }

            return bestMove;
        }

        private void ScoreCaptures()
        {
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                Move move = moves[i];

                if (move.Type == MoveType.EnPassant)
                {
                    values[i] = MvvLvaValues[PieceType.Pawn][PieceType.Pawn];
                    continue;
                }

                byte attacker = board.PieceTypes[move.OriginIndex];
                byte victim   = board.PieceTypes[move.TargetIndex];

                values[i] = MvvLvaValues[attacker][victim];
            }
        }

        private void ScoreQuiets()
        {
            for (int i = moves.Count - 1; i >= 0; i--)
                values[i] = historyHeuristic.Read(board.Side, moves[i]);
        }
    }
}
