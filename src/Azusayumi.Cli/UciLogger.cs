using Azusayumi.Core.GameLogic;
using Azusayumi.Core.Search;

namespace Azusayumi.Cli
{
    internal struct UciLogger : ILogger
    {
        public static void LogFullInfo(SearchInfo info, ReadOnlySpan<Move> pv)
        {
            Span<char> log = stackalloc char[512];
            int offset = 0;

            "info".CopyTo(log);
            offset += "info".Length;

            " depth ".CopyTo(log[offset..]);
            offset += " depth ".Length;
            info.Depth.TryFormat(log[offset..], out int written);
            offset += written;

            " seldepth ".CopyTo(log[offset..]);
            offset += " seldepth ".Length;
            info.HighestDepth.TryFormat(log[offset..], out written);
            offset += written;

            " multipv ".CopyTo(log[offset..]);
            offset += " multipv ".Length;
            info.PVIndex.TryFormat(log[offset..], out written);
            offset += written;

            " score ".CopyTo(log[offset..]);
            offset += " score ".Length;
            if (SearchManager.IsMateScore(info.Score))
            {
                "mate ".CopyTo(log[offset..]);
                offset += "mate ".Length;
                SearchManager.GetMateDistance(info.Score).TryFormat(log[offset..], out written);
                offset += written;
            }
            else
            {
                "cp ".CopyTo(log[offset..]);
                offset += "cp ".Length;
                info.Score.TryFormat(log[offset..], out written);
                offset += written;
            }

            " nodes ".CopyTo(log[offset..]);
            offset += " nodes ".Length;
            info.Nodes.TryFormat(log[offset..], out written);
            offset += written;

            " nps ".CopyTo(log[offset..]);
            offset += " nps ".Length;
            long nps = 1000 * info.Nodes / Math.Max(1, info.Time);
            nps.TryFormat(log[offset..], out written);
            offset += written;

            " time ".CopyTo(log[offset..]);
            offset += " time ".Length;
            info.Time.TryFormat(log[offset..], out written);
            offset += written;

            " pv".CopyTo(log[offset..]);
            offset += " pv".Length;
            for (int i = 0; i < pv.Length; i++)
            {
                log[offset++] = ' ';
                pv[i].Format(log[offset..], out written);
                offset += written;
            }

            Console.WriteLine(log[0..offset]);
        }

        public static void LogCurrentMove(int depth, Move move, int moveCount)
        {
            Span<char> log = stackalloc char[64];
            int offset = 0;

            "info".CopyTo(log);
            offset += "info".Length;

            " depth ".CopyTo(log[offset..]);
            offset += " depth ".Length;
            depth.TryFormat(log[offset..], out int written);
            offset += written;

            " currmove ".CopyTo(log[offset..]);
            offset += " currmove ".Length;
            move.Format(log[offset..], out written);
            offset += written;

            " currmovenumber ".CopyTo(log[offset..]);
            offset += " currmovenumber ".Length;
            moveCount.TryFormat(log[offset..], out written);
            offset += written;

            Console.WriteLine(log[0..offset]);
        }
    }
}
