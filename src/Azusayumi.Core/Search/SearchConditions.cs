namespace Azusayumi.Core.Search
{
    public readonly record struct SearchConditions(
        int  Depth,
        int  Time,
        int  Inc,
        int  MoveTime,
        long Nodes);
}
