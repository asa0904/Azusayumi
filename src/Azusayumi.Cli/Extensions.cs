using Azusayumi.Core.GameLogic;
using System.Runtime.CompilerServices;

namespace Azusayumi.Cli
{
    internal static class ReadOnlySpanExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<char> ConsumeTo(this ReadOnlySpan<char> source, char separator, out ReadOnlySpan<char> token)
        {
            int index = source.IndexOf(separator);
            if (index == -1)
            {
                token = source;
                return default;
            }
            else
            {
                token = source[..index];
                return source[(index + 1)..];
            }
        }
    }

    internal static class MoveExtensions
    {
        internal static bool Equals(this Move move, ReadOnlySpan<char> chars)
        {
            if (move.Type == MoveType.Promotion)
            {
                if (chars.Length < 5) { return false; }

                char promotionType = "pnbrq"[move.PromotionType];
                if (chars[4] != promotionType) { return false; }
            }
            else
            {
                if (chars.Length != 4) { return false; }
            }

            int originIndex = move.OriginIndex;
            int targetIndex = move.TargetIndex;

            if (move.Type == MoveType.Castling)
            {
                targetIndex = originIndex < targetIndex ? originIndex + 2 : originIndex - 2;
            }

            if (chars[3] != (char)('1' + Square.GetRank(targetIndex))) { return false; }
            if (chars[2] != (char)('a' + Square.GetFile(targetIndex))) { return false; }
            if (chars[1] != (char)('1' + Square.GetRank(originIndex))) { return false; }
            if (chars[0] != (char)('a' + Square.GetFile(originIndex))) { return false; }

            return true;
        }

        internal static void Format(this Move move, Span<char> destination, out int written)
        {
            if (move == Move.Null)
            {
                "0000".CopyTo(destination);
                written = 4;
                return;
            }

            int originIndex = move.OriginIndex;
            int targetIndex = move.TargetIndex;

            if (move.Type == MoveType.Castling)
            {
                targetIndex = originIndex < targetIndex ? originIndex + 2 : originIndex - 2;
            }

            destination[3] = (char)('1' + Square.GetRank(targetIndex));
            destination[2] = (char)('a' + Square.GetFile(targetIndex));
            destination[1] = (char)('1' + Square.GetRank(originIndex));
            destination[0] = (char)('a' + Square.GetFile(originIndex));
            written = 4;

            if (move.Type == MoveType.Promotion)
            {
                destination[4] = "pnbrq"[move.PromotionType];
                written = 5;
            }
        }

        internal static string ToUciString(this Move move)
        {
            Span<char> uciMove = new char[move.Type == MoveType.Promotion ? 5 : 4];
            move.Format(uciMove, out _);

            return uciMove.ToString();
        }
    }
}
