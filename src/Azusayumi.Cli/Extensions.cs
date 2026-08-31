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
}
