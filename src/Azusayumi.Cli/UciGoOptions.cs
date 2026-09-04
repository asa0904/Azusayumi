namespace Azusayumi.Cli
{
    internal record struct UciGoOptions(
        int  Depth,
        int  MoveTime,
        int  Nodes,
        int  Wtime,
        int  Btime,
        int  Winc,
        int  Binc,
        bool IsInfinite);
}
