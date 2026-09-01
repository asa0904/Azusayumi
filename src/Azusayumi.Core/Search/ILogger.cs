using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Search
{
    public interface ILogger
    {
        static abstract void Log(SearchInfo info, ReadOnlySpan<Move> pv);
    }
}
