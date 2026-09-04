namespace Azusayumi.Core.Search
{
    public record struct SearchInfo(
        int  Depth,
        int  HighestDepth,
        int  Score,
        long Nodes,
        long Time);
}
