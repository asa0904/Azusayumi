using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal record struct SearchInfo(
        int  Depth,
        int  HighestDepth,
        int  Score,
        long Nodes,
        long Time,
        Move BestMove,
        Move PonderMove);
}
