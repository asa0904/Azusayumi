namespace Azusayumi.GameLogic
{
    internal class MoveList
    {
        public byte Count = 0;
        private readonly Move[] moves = new Move[256]; // あり得る最大手数は218手と言われている

        public Move this[int index]
        {
            get => moves[index];

            set => moves[index] = value;
        }

        public void Add(Move move)
        {
            moves[Count++] = move;
        }

        public void Clear()
        {
            Count = 0;
        }
    }
}