using Azusayumi.Evaluation;
using Azusayumi.GameLogic;

namespace Azusayumi.Search
{
    internal class Searcher(int sizeMB)
    {
        private const int Infinity  = 10000000;
        private const int MateValue = 10000;
        private const int DrowValue = 0;
        private const int MaxPly    = 64;

        private const           int   AspirationDelta  = 50;
        private static readonly int[] FutilityMargines = [0, 200, 300, 500];

        private readonly TranspositionTable transpositionTable = new(sizeMB);
        private readonly TriangularPVTable  triangularPVTable  = new(MaxPly);
        private readonly KillerHeuristic    killerHeuristic    = new(MaxPly);
        private readonly HistoryHeuristic   historyHeuristic   = new();
        private readonly Limits             limits             = new();

        public void Clear()
        {
            transpositionTable.Clear();
            killerHeuristic.Clear();
            historyHeuristic.Clear();
            Evaluator.Clear();
        }

        public void Stop()
        {
            limits.IsPondering = false;
            limits.IsOver      = true;
        }

        public void PonderHit()
        {
            limits.IsPondering = false;
        }

        public void PrintTTEntry(Board board)
        {
            TTEntry entry = transpositionTable.Read(board);
            Console.WriteLine(entry.Key == board.State.Key ? entry : "no entry");
        }

        public void Search(Board board, int time, int inc, int moveTime, int maxDepth, int maxNodes, bool isPonder)
        {
            limits.Set(time, inc, moveTime, maxNodes, isPonder);

            int depth = 0;
            int alpha, beta, value = 0;
            Move bestMove = Move.Null, ponderMove = Move.Null;

            // Iterative Deepening
            while (!limits.IsOver && ++depth <= maxDepth)
            {
                int delta = AspirationDelta;

                if (depth > 5)
                {
                    alpha = Math.Max(value - delta, -Infinity);
                    beta  = Math.Min(value + delta,  Infinity);
                }
                else
                {
                    alpha = -Infinity;
                    beta  =  Infinity;
                }

                // Aspiration Windows
                while (true)
                {
                    value = AlphaBeta(board, depth, ply: 0, alpha, beta, isPV: true, isNullMove: false);

                    if (limits.IsOver) break;

                    if (value > alpha && value < beta)
                    {
                        bestMove   = triangularPVTable.BestMove;
                        ponderMove = triangularPVTable.PonderMove;
                        PrintLogs(depth, value);
                        break;
                    }

                    // Aspiration Window 失敗
                    if (value <= alpha)
                    {
                        delta *= 2;
                        alpha = Math.Max(value - delta, -Infinity);
                    }
                    else
                    {
                        delta *= 2;
                        beta = Math.Min(value + delta, Infinity);
                    }
                }
            }

            limits.Stopwatch.Stop();

            Console.WriteLine($"bestmove {bestMove} ponder {ponderMove}");
        }

        private int Quiesce(Board board, int ply, int alpha, int beta)
        {
            limits.Nodes++;

            int standPat = Evaluator.Evaluate(board);
            int bestValue = standPat;

            if (standPat >= beta || ply >= MaxPly) return standPat;

            if (standPat > alpha) alpha = standPat;

            MoveList moves = new();
            MoveGenerator.GenerateCaptureMoves(moves, board);
            MovePicker.SortCaptures(moves, board);

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];

                board.MakeMove(move);
                int value = -Quiesce(board, ply + 1, -beta, -alpha);
                board.UnmakeMove(move);

                if (value >= beta) return value;

                if (value > alpha) alpha = value;

                if (value > bestValue) bestValue = value;
            }
            
            return bestValue;
        }

        private int AlphaBeta(Board board, int depth, int ply, int alpha, int beta, bool isPV, bool isNullMove)
        {
            // 探索制限の確認
            limits.Check();
            if (limits.IsOver) return 0;

            triangularPVTable.Clear(ply);

            // Mate Distance Pruning
            alpha = Math.Max(alpha, -MateValue + ply);
            beta  = Math.Min(beta, MateValue - ply - 1);
            if (alpha >= beta) return alpha;

            // ドローの場合
            if (board.State.IsThreefold
            ||  board.State.HalfMoveClock >= 100
            || (board.State.PawnKey == Zobrist.NoPawnKey && board.State.Phases[0] + board.State.Phases[1] <= 1))
                return DrowValue;

            bool isChecked = board.IsChecked();

            // Check Extensions
            if (isChecked) depth++;

            // 葉ノードの場合
            if (depth == 0) return Quiesce(board, ply, alpha, beta);

            // 置換表参照
            var ttEntry = transpositionTable.Read(board);
            var ttMove  = Move.Null;
            if (ttEntry.Key == board.State.Key)
            {
                ttMove = ttEntry.Move;

                if (!isPV && ttEntry.Depth >= depth)
                {
                    if (Math.Abs(ttEntry.Value) > MateValue - MaxPly)
                        ttEntry.Value += ttEntry.Value > 0 ? -ply : ply;

                    switch (ttEntry.NodeType)
                    {
                        case NodeType.PV:
                            return ttEntry.Value;

                        case NodeType.Cut:
                            alpha = Math.Max(alpha, ttEntry.Value);
                            break;

                        case NodeType.All:
                            beta = Math.Min(beta, ttEntry.Value);
                            break;
                    }

                    if (alpha >= beta) return ttEntry.Value;
                }
            }

            int value;
            int staticEvalation = Evaluator.FastEvaluate(board);

            //Reverse Futility Pruning
            if (!isPV
             && !isChecked
             && depth > 3
             && Math.Abs(beta - 1) < MateValue - MaxPly)
            {
                int margin = 120 * depth;
                if (staticEvalation - margin >= beta) return staticEvalation - margin;
            }

            // Internal Iterative Deepeinig
            if (isPV
             && depth > 5
             && ttMove == Move.Null)
            {
                _ = AlphaBeta(board, depth - 2, ply, alpha, beta, isPV, isNullMove: false);
                ttMove = triangularPVTable.Read(ply);
            }

            // Null Move Pruning
            if (!isPV
             && !isNullMove
             && !isChecked
             && depth > 2
             && staticEvalation >= beta
             && board.State.Phases[board.Side] > 0)
            {
                int R = depth > 6 ? 3 : 2;

                board.MakeNullMove();
                value = -AlphaBeta(board, depth - R - 1, ply + 1, -beta, -(beta - 1), isPV, isNullMove: true);
                board.UnmakeNullMove();

                if (limits.IsOver) return 0;

                if (value >= beta) return value;
            }

            // Futility Pruning
            bool canFutilityPruning = false;
            if (!isPV
             && !isChecked
             && depth <= 3
             && Math.Abs(alpha) < MateValue - MaxPly
             && staticEvalation + FutilityMargines[depth] <= alpha)
            {
                canFutilityPruning = true;
            }

            int moveCount  = 0;
            int bestValue  = -Infinity;
            var bestMove   = Move.Null;
            var quietMoves = new MoveList();
            var nodeType   = NodeType.All;

            MovePicker picker = new(board, ttMove,
                killerHeuristic.Read(ply, slot: 0), killerHeuristic.Read(ply, slot: 1), historyHeuristic);

            limits.Nodes++;

            Move move;
            while ((move = picker.Pick()) != Move.Null)
            {
                moveCount++;

                bool isQuiet = board.PieceTypes[move.TargetIndex] == PieceType.None && move.Type == MoveType.Quiet;

                if (isQuiet) quietMoves.Add(move);

                board.MakeMove(move);

                // Futility Pruning
                if (canFutilityPruning
                    && isQuiet
                    && moveCount > 1)
                {
                    board.UnmakeMove(move);
                    continue;
                }

                // Late Move Reduction
                if (!isPV
                 && !isChecked
                 && depth > 2
                 && moveCount > 4
                 && !picker.IsKiller)
                {
                    int R = moveCount > 9 ? 2 : 1;
                    value = -AlphaBeta(board, depth - R - 1, ply + 1, -(alpha + 1), -alpha, isPV, isNullMove);
                }
                else value = alpha + 1;

                // NegaScout
                if (value > alpha)
                {
                    if (moveCount == 1)
                        value = -AlphaBeta(board, depth - 1, ply + 1, -beta, -alpha, isPV, isNullMove);

                    else
                    {
                        // Null Window Search
                        value = -AlphaBeta(board, depth - 1, ply + 1, -(alpha + 1), -alpha, isPV: false, isNullMove);

                        if (value > alpha && value < beta)
                            value = -AlphaBeta(board, depth - 1, ply + 1, -beta, -alpha, isPV: true, isNullMove);
                    }
                }

                board.UnmakeMove(move);

                if (limits.IsOver) return 0;

                if (value > bestValue)
                {
                    bestValue = value;

                    if (value > alpha)
                    {
                        nodeType = NodeType.PV;
                        bestMove = move;

                        triangularPVTable.Write(ply, move);

                        // Beta Cutoff
                        if (value >= beta)
                        {
                            nodeType = NodeType.Cut;

                            if (isQuiet)
                            {
                                // Killer Heuristic
                                killerHeuristic.Write(move, ply);

                                // History Heuristic
                                historyHeuristic.Update(board.Side, move, bonus: depth * depth);

                                for (int i = quietMoves.Count - 2; i >= 0; i--)
                                    historyHeuristic.Update(board.Side, quietMoves[i], bonus: -(depth * depth));
                            }

                            break;
                        }

                        alpha = value;
                    }
                }
            }

            // 終局の場合
            if (moveCount == 0) return isChecked ? -MateValue + ply : DrowValue;

            // 置換表登録
            transpositionTable.Write(nodeType, board, depth, bestValue, bestMove);

            return bestValue;
        }

        private void PrintLogs(int depth, int value)
        {
            string score;
            if (Math.Abs(value) < MateValue - MaxPly) score = $"cp {value}";
            else
            {
                int ply = value > 0 ? (MateValue - value + 1) : (-MateValue - value);
                score = $"mate {ply / 2}";
            }

            Console.WriteLine($"info depth {depth} "
                + $"score {score} "
                + $"nodes {limits.Nodes} "
                + $"nps {1000 * (long)limits.Nodes / Math.Max(limits.Stopwatch.ElapsedMilliseconds, 1)} "
                + $"time {limits.Stopwatch.ElapsedMilliseconds} "
                + $"pv {triangularPVTable}");
        }
    }
}
