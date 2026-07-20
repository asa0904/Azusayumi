namespace Azusayumi.Core.Evaluation
{
    internal class Weights
    {
        internal static readonly Score[] Material  = new Score[5];
        internal static readonly Score[] PawnPst   = new Score[64];
        internal static readonly Score[] KnightPst = new Score[64];
        internal static readonly Score[] BishopPst = new Score[64];
        internal static readonly Score[] RookPst   = new Score[64];
        internal static readonly Score[] QueenPst  = new Score[64];
        internal static readonly Score[] KingPst   = new Score[64];

        internal static readonly Score[] KnightMobility = new Score[9];
        internal static readonly Score[] BishopMobility = new Score[14];
        internal static readonly Score[] RookMobility   = new Score[15];
        internal static readonly Score[] QueenMobility  = new Score[28];
    }
}
