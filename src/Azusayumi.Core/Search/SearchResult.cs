using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    public readonly record struct SearchResult(Move BestMove, Move PonderMove);
}
