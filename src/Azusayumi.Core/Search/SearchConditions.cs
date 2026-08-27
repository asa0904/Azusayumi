namespace Azusayumi.Core.Search
{
    internal readonly record struct SearchConditions(
        int  Depth,
        int  Time,
        int  Inc,
        int  MoveTime,
        long Nodes);
}
