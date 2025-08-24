namespace Azusayumi.GameLogic
{
    internal class GameState
    {
        public ulong Key;
        public ulong PawnKey;
        public byte  CastleRights;
        public byte  EnPassantIndex;
        public byte  HalfMoveClock;
        public int   MidgameValue;
        public int   EndgameValue;
        public readonly byte[] Phases;

        public byte CapturedPiece;
        public bool IsThreefold;
        public byte KingIndex;

        public GameState()
        {
            Phases = new byte[2];
            CapturedPiece = PieceType.None;
        }

        public GameState Initialize(GameState previous)
        {
            Key            = previous.Key;
            PawnKey        = previous.PawnKey;
            CastleRights   = previous.CastleRights;
            EnPassantIndex = previous.EnPassantIndex;
            HalfMoveClock  = previous.HalfMoveClock;
            MidgameValue   = previous.MidgameValue;
            EndgameValue   = previous.EndgameValue;

            Phases[Color.White] = previous.Phases[Color.White];
            Phases[Color.Black] = previous.Phases[Color.Black];

            CapturedPiece = PieceType.None;
            IsThreefold   = false;

            return this;
        }
    }
}
