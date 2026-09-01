namespace Azusayumi.Core.Evaluation
{
    public class Weights
    {
        internal static readonly Score[] Material  = [(100, 100), (325, 325), (350, 350), (500, 500), (900, 900)];
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

        public static int GetLength(Term term)
        {
            return term switch
            {
                Term.Material       => Material.Length,
                Term.PawnPst        => PawnPst.Length,
                Term.KnightPst      => KnightPst.Length,
                Term.BishopPst      => BishopPst.Length,
                Term.RookPst        => RookPst.Length,
                Term.QueenPst       => QueenPst.Length,
                Term.KingPst        => KingPst.Length,
                Term.KnightMobility => KnightMobility.Length,
                Term.BishopMobility => BishopMobility.Length,
                Term.RookMobility   => RookMobility.Length,
                Term.QueenMobility  => QueenMobility.Length,
                _ => 1
            };
        }

        public static (int Mid, int End) GetValue(Term term, int index)
        {
            return term switch
            {
                Term.Material       => Material[index],
                Term.PawnPst        => PawnPst[index],
                Term.KnightPst      => KnightPst[index],
                Term.BishopPst      => BishopPst[index],
                Term.RookPst        => RookPst[index],
                Term.QueenPst       => QueenPst[index],
                Term.KingPst        => KingPst[index],
                Term.KnightMobility => KnightMobility[index],
                Term.BishopMobility => BishopMobility[index],
                Term.RookMobility   => RookMobility[index],
                Term.QueenMobility  => QueenMobility[index],
                _ => Score.Zero
            };
        }
    }
}
