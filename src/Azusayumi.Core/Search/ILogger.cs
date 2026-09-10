using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Core.Search
{
    public interface ILogger
    {
        static abstract void LogFullInfo(SearchInfo info, ReadOnlySpan<Move> pv);

        static abstract void LogBestMove(SearchResult result);

        static abstract void LogCurrentMove(int depth, Move move, int moveCount);

        static abstract void LogException(Exception ex);
    }

    internal readonly struct NullLogger : ILogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogFullInfo(SearchInfo info, ReadOnlySpan<Move> pv)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogBestMove(SearchResult result)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogCurrentMove(int depth, Move move, int moveCount)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(Exception ex)
        {
            return;
        }
    }
}
