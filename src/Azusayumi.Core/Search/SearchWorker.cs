using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    internal partial class SearchWorker
    {
        internal const int MaxPly = 64;

        private const int Infinity  = short.MaxValue;
        private const int MateValue = 10000;
        private const int DrawValue = 0;

        private readonly Board _board = new();
    }
}
