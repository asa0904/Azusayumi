using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        private int PVSearch<TColor>(int depth, int ply, int alpha, int beta) where TColor : struct, IColor
        {
            if ((_nodes & 1023) == 0 && _manager.ShouldStop()) { return DrawValue; }

            if (depth == 0)
            {
#if COLLECT_STATS
                _statistics.HorizonNodes++;
#endif
                return QuiescePV<TColor>(ply, alpha, beta);
            }

            _nodes++;
            _pvTable.Clear(ply);

#if COLLECT_STATS
            _statistics.InteriorNodes++;
#endif

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            Move ttMove = Move.Null;
            if (_manager.TranspositionTable.TryRead(_board.Key, out TTEntry ttEntry))
            {
                ttMove = ttEntry.Move;
            }

            int      bestValue = -Infinity;
            Move     bestMove  = Move.Null;
            NodeType nodeType  = NodeType.All;
            bool     isInCheck = _board.IsInCheck<TColor>();

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            MoveGenerator<TColor>.GenerateLegalMoves(ref buffer, _board, isInCheck);
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.Score<TColor>(scoredMoves, ttMove, _killerTable[ply, 0], _killerTable[ply, 1], _historyTable, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);

                int value;
                if (i == 0)
                {
                    value = -OppositePVSearch<TColor>(depth - 1, ply + 1, -beta, -alpha);
                }
                else
                {
                    value = -OppositeNullWindowSearch<TColor>(depth - 1, ply + 1, -alpha);

                    if (value > alpha && value < beta)
                    {
#if COLLECT_STATS
                        _statistics.ResearchCount++;
                        long nodesBefore = _statistics.TotalNodes;
#endif

                        value = -OppositePVSearch<TColor>(depth - 1, ply + 1, -beta, -alpha);

#if COLLECT_STATS
                        long nodesSpent = _statistics.TotalNodes - nodesBefore;
                        _statistics.ResearchNodes += nodesSpent;
#endif
                    }
                }

                _board.UnmakeMove<TColor>(move);

                if (_manager.IsOver) { return DrawValue; }

                if (value > bestValue)
                {
                    bestValue = value;
                    bestMove  = move;

                    if (value > alpha)
                    {
                        if (value >= beta)
                        {
#if COLLECT_STATS
                            _statistics.CutNodes++;
                            if (i == 0) { _statistics.FirstCutNodes++; }
#endif

                            nodeType = NodeType.Cut;

                            if (_board.IsQuiet(move))
                            {
                                _killerTable.Write(move, ply);
                                _historyTable.Update<TColor>(move.Key, bonus: depth * depth);
                            }

                            break;
                        }

                        nodeType = NodeType.PV;

                        alpha = value;
                        _pvTable.Write(ply, move);
                    }
                }
            }

            if (scoredMoves.Length == 0) { return isInCheck ? -MateValue + ply : DrawValue; }

            _manager.TranspositionTable.Write(_board.Key, GetTTWriteValue(bestValue, ply), bestMove, nodeType, depth);

            return bestValue;
        }

        private int NullWindowSearch<TColor>(int depth, int ply, int beta) where TColor : struct, IColor
        {
            if ((_nodes & 1023) == 0 && _manager.ShouldStop()) { return DrawValue; }

            if (depth == 0)
            {
#if COLLECT_STATS
                _statistics.HorizonNodes++;
#endif
                return QuiesceNonPV<TColor>(ply, beta);
            }

            _nodes++;

#if COLLECT_STATS
            _statistics.InteriorNodes++;
#endif

            if (_board.IsDraw() || ply >= MaxPly) { return DrawValue; }

            Move ttMove = Move.Null;
            if (_manager.TranspositionTable.TryRead(_board.Key, out TTEntry ttEntry))
            {
                if (ttEntry.Depth >= depth)
                {
                    int ttValue = GetTTReadValue(ttEntry.Value, ply);
                    if (ttEntry.NodeType == NodeType.PV
                    || (ttEntry.NodeType == NodeType.Cut && ttValue >= beta)
                    || (ttEntry.NodeType == NodeType.All && ttValue < beta))
                    {
                        return ttValue;
                    }
                }

                ttMove = ttEntry.Move;
            }

            int      bestValue = -Infinity;
            Move     bestMove  = Move.Null;
            NodeType nodeType  = NodeType.All;
            bool     isInCheck = _board.IsInCheck<TColor>();

            MoveBuffer buffer = new(_moveArrayPool.GetSpan(ply));
            MoveGenerator<TColor>.GenerateLegalMoves(ref buffer, _board, isInCheck);
            Span<ScoredMove> scoredMoves = buffer.AsSpan();
            MoveOrdering.Score<TColor>(scoredMoves, ttMove, _killerTable[ply, 0], _killerTable[ply, 1], _historyTable, _board);

            for (int i = 0; i < scoredMoves.Length; i++)
            {
                Move move = MoveOrdering.Select(i, scoredMoves);

                _board.MakeMove<TColor>(move);
                int value = -OppositeNullWindowSearch<TColor>(depth - 1, ply + 1, -(beta - 1));
                _board.UnmakeMove<TColor>(move);

                if (_manager.IsOver) { return DrawValue; }

                if (value > bestValue)
                {
                    bestValue = value;
                    bestMove  = move;

                    if (value >= beta)
                    {
#if COLLECT_STATS
                        _statistics.CutNodes++;
                        if (i == 0) { _statistics.FirstCutNodes++; }
#endif

                        nodeType = NodeType.Cut;

                        if (_board.IsQuiet(move))
                        {
                            _killerTable.Write(move, ply);
                            _historyTable.Update<TColor>(move.Key, bonus: depth * depth);
                        }

                        break;
                    }
                }
            }

            if (scoredMoves.Length == 0) { return isInCheck ? -MateValue + ply : DrawValue; }

            _manager.TranspositionTable.Write(_board.Key, GetTTWriteValue(bestValue, ply), bestMove, nodeType, depth);

            return bestValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int OppositePVSearch<TColor>(int depth, int ply, int alpha, int beta)
            where TColor : struct, IColor
        {
            return TColor.IsWhite ? PVSearch<Black>(depth, ply, alpha, beta)
                                  : PVSearch<White>(depth, ply, alpha, beta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int OppositeNullWindowSearch<TColor>(int depth, int ply, int beta)
            where TColor : struct, IColor
        {
            return TColor.IsWhite ? NullWindowSearch<Black>(depth, ply, beta)
                                  : NullWindowSearch<White>(depth, ply, beta);
        }
    }
}
