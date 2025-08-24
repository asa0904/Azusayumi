using System.Diagnostics;

namespace Azusayumi.Search
{
    internal class Limits
    {
        public bool IsOver;
        public bool IsPondering;
        public int  Nodes;
        public readonly Stopwatch Stopwatch = new();

        private int callCount;
        private int moveTime;
        private int maxNodes;
        
        public void Set(int time, int inc, int moveTime, int maxNodes, bool isPonder)
        {
            Stopwatch.Restart();

            IsOver      = false;
            IsPondering = isPonder;
            Nodes       = 0;
            
            callCount = 1024;
            this.moveTime = CalculateMoveTime(time, inc, moveTime);
            this.maxNodes = maxNodes;
        }

        public void Check()
        {
            if (IsPondering || IsOver || callCount-- > 0) return;

            if (Stopwatch.ElapsedMilliseconds > moveTime || Nodes >= maxNodes)
            {
                IsOver = true;
                Stopwatch.Stop();
                return;
            }

            callCount = 1024;
        }

        private static int CalculateMoveTime(int time, int inc, int moveTime)
        {
            if (time <= 2 * inc) inc = 0;
            return Math.Min(time / 20 + inc / 2, moveTime) - 10;
        }
    }
}
